# Day 3 Summary — Web APIs with .NET (June 10, 2026)

Day 3 was about depth and breadth. We started by cementing our understanding of HTTP as a *resource-oriented* architecture and the principles that separate well-designed APIs from ones that merely work. From there we completed the full CRUD surface for the Vendors resource, tackled HTTP caching and intermediaries, introduced **Minimal APIs** as a lighter-weight alternative to controllers, and finished the day by wiring up a new nested Catalog resource using a production-style feature organization pattern — including a taste of C# 14's new `extension` block syntax. By end of day you had a substantially richer API and a concrete mental framework for API design decisions you'll face on every project.

---

## 1. HTTP Resource Design — The Conceptual Foundation

We invested real time in `notes/http-usage.md` to make sure the *why* behind our API decisions is solid.

### Resources Are Nouns. Verbs Are the HTTP Methods.

HTTP is **resource-oriented**. A resource is any important "thing" your system manages that deserves a name (URL). The verbs (`GET`, `POST`, `PUT`, `DELETE`) are fixed — you don't invent them. If you find a verb leaking into a URL name like `/createVendor` or `/deleteItem`, that's a signal something is off.

```
https://api.company.com/hr/employees/13
│         │              │            │
scheme    authority      path         (identifies the resource)
```

### The Two Primary Resource Types

**Collections** — a list of things: `/employees`, `/vendors`, `/pets`
- `GET` — retrieve a representation of the list
- `POST` — "please add this to the collection"
- `PUT` — replace the entire collection
- `DELETE` — remove the entire collection (rarely used in practice)

**Documents** — a single thing, usually subordinate to a collection: `/employees/{id}`
- `GET` — retrieve it
- `DELETE` — remove it from the collection
- `PUT` — replace the whole document with a new representation
- `POST` — "process this against the document" (rare, but valid)

**Hybrids** mix collection and document semantics on subordinate paths:
```
/employees/{id}/manager          → document (one manager)
/employees/{id}/subordinates     → collection (multiple)
```

### Steve Klabnik's Rule

> *"Almost every API design problem can be solved by adding a resource."*

When you're not sure how to model a complex action (raise, termination, credit increase), make it a **noun**. Instead of trying to PATCH a credit limit with some opaque action, model the intent as a resource:

```http
POST /customers/{id}/credit-updates
Content-Type: application/json

{ "increase": 205 }

→ 201 Created
Location: /customers/{id}/credit-updates/{updateId}

{ "increase": 205, "status": "applied" }
```

Now it's auditable, navigable, and unambiguous.

### Backwards Compatibility — "Quiet Quitting"

When clients are consuming your API, they should only react to what they're explicitly looking for — not to every field in the response. This is called being a **good consumer**: if your API adds new optional fields, well-behaved clients ignore them. The corollary for *producers*: don't remove or rename fields; add new ones instead. We called this "Quiet Quitting" — only work to the job description. The HATEOAS `_links` pattern in `docs/software.http` demonstrates one approach to making API evolution explicit.

---

## 2. Dependency Injection — Wiring Up Real Services

We implemented `HttpContextRequestinUserLookup`, the real production implementation of the `ILookupRequestingUsers` interface first introduced in Day 2.

### The Real Implementation

```csharp
// src/SoftwareSolution/Software.Api/Vendors/HttpContextRequestinUserLookup.cs

public class HttpContextRequestinUserLookup(IHttpContextAccessor httpContextAccessor)
    : ILookupRequestingUsers
{
    public string GetRequestingUserId()
    {
        if (httpContextAccessor.HttpContext is null ||
            httpContextAccessor.HttpContext.User is null)
        {
            throw new InvalidOperationException(
                "Use this in the context of an Http request only");
        }
        return httpContextAccessor.HttpContext.User.Identity?.Name ?? string.Empty;
    }
}
```

### Registering the Service with a Lifetime

In `Program.cs`:
```csharp
builder.Services.AddSingleton<TimeProvider>(sp => TimeProvider.System); // one forever
builder.Services.AddScoped<ILookupRequestingUsers, HttpContextRequestinUserLookup>(); // one per request
builder.Services.AddHttpContextAccessor(); // required for IHttpContextAccessor to work
```

**Service lifetimes are important.** `HttpContextRequestinUserLookup` depends on `IHttpContextAccessor`, which is inherently per-request. So we register it as `Scoped` — one instance per HTTP request. If you register something per-request (Scoped) inside a Singleton, you get a *captive dependency* bug at runtime. The general rule:
- **Singleton** — stateless, share forever (e.g., `TimeProvider`, configuration)
- **Scoped** — stateful per-request (e.g., Marten sessions, identity lookups)
- **Transient** — new instance every time it's requested (rare; use when there's no shared state to worry about)

**Why have the interface at all?** In tests, we inject a fake — a deterministic, controllable stand-in — without touching any production code. The interface is the seam that makes that possible.

---

## 3. Completing CRUD for Vendors

### Refactoring: Namespace and File Organization

The `Software/Api.cs` file had grown to include entity, model, and controller code all in one file. We refactored it: moved everything into a dedicated `Vendors/` folder, separated `VendorEntity.cs` and `Models.cs`, and updated the namespace to `Software.Api.Vendors`. This isn't just tidiness — co-locating code by *feature* rather than by *type* makes it easier to find and change related things together.

### GET /vendors — Paginated List

```csharp
[HttpGet("/vendors")]
public async Task<ActionResult> GetVendorsAsync(
    [FromServices] IDocumentSession session,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] string name = "",
    CancellationToken token = default)
{
    var results = await session.Query<VendorEntity>()
        .OrderBy(v => v.Name)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(v => new VendorSummaryModel
        {
            Id = v.Id,
            Name = v.Name,
            Url = v.Url
        })
        .ToListAsync(token);

    return Ok(new VendorSummary
    {
        Vendors = results,
        Page = page,
        PageSize = pageSize
    });
}
```

Key things to notice:
- Pagination via `Skip`/`Take` using `[FromQuery]` parameters with sensible defaults
- The projection to `VendorSummaryModel` happens in the database query — Salary-equivalent fields never leave storage
- `CancellationToken` is threaded through so the request can be cleanly cancelled if the client disconnects
- The response wraps the list in a `VendorSummary` object so we can add metadata (page, size) — and later, total counts or links — without breaking clients

### DELETE /vendors/{id} — Design Considerations

```csharp
[HttpDelete("/vendors/{id:guid}")]
public async Task<ActionResult> DeleteAsync(Guid id, [FromServices] IDocumentSession session)
{
    // Rules discussed:
    // - Only the person who created the vendor can delete it → otherwise 403
    // - Can't delete if it has associated catalog items → 409 Conflict
    return NoContent();
}
```

The stub shows the shape, but the design discussion was the real lesson:
- `DELETE` that succeeds returns **`204 No Content`** — there's nothing to return, the thing is gone
- Business rules like "can't delete if in use" are a `409 Conflict`, not a generic error
- Idempotency: a second `DELETE` on an already-deleted resource can legitimately return `404` — this is fine and expected

### PUT /vendors/{id}/point-of-contact — Partial Sub-Resource Update

Rather than a full `PUT /vendors/{id}` (which would replace the entire vendor), we used a "miniput" pattern — updating a single sub-resource:

```csharp
[HttpPut("/vendors/{id:guid}/point-of-contact")]
public async Task<ActionResult> ReplacePointOfContactAsync(
    Guid id,
    [FromServices] IDocumentSession session,
    [FromBody] VendorPointOfContact request)
{
    var savedVendor = await session.Query<VendorEntity>()
        .Where(v => v.Id == id)
        .SingleOrDefaultAsync();

    if (savedVendor is null) { return NotFound(); }

    savedVendor.PointOfContact = request; // mutate the property (requires `set`, not `init`)
    session.Store(savedVendor);
    await session.SaveChangesAsync();

    return NoContent();
}
```

We had to adjust `VendorEntity` to allow `PointOfContact` to be mutated:
```csharp
// Changed from `init` to `set` on the entity because we update it
public required VendorPointOfContact PointOfContact { get; set; }
```

`PUT` **replaces** the sub-resource entirely. The client sends the complete new point-of-contact. If you want partial updates, that's `PATCH` — a different method and semantics entirely.

---

## 4. HTTP Intermediaries and Caching

We drew out the landscape between a client and a server (`notes/intermediaries.excalidraw`), covering:

- **User-Agent** → request → intermediary (load balancer, proxy, CDN) → **Origin Server**
- Intermediaries may be transparent or can cache, modify, or route traffic
- `Cache-Control` response headers (e.g., `public, max-age=3600`) are instructions to these intermediaries — browsers, CDNs, and reverse proxies all honour them
- `.NET's HttpClient` does **not** cache by default — it is a raw transport layer. Caching at the client level requires additional middleware or libraries

```http
# From docs/software.http — response headers discussed
Location: /vendors/38938
Cache-Control: public, max-age: 3200
```

The key insight: `Cache-Control` headers don't just affect the browser. Every proxy and CDN between client and server reads these headers and decides whether to serve a cached copy. Designing your responses with cacheability in mind is a performance lever you get for free.

---

## 5. Catalog Items — Nested Resource Design

We documented the Catalog resource in `docs/catalog.md`:

- Catalog items belong to a specific vendor: `POST /vendors/{id}/catalog`
- Adding a catalog item requires: (a) the user is a Software Center employee, (b) the referenced vendor must exist
- If the vendor doesn't exist → `404 Not Found` (not a generic error)

```http
POST /vendors/fba8081a-95aa-4b3d-8a81-b1749f14fe2a/catalog
Content-Type: application/json

{ "name": "Microsoft Word" }
```

The design principle: **prefer HTTP status codes as your error signal, not inventing your own error codes or formats.** The standard codes already communicate what went wrong — use them.

---

## 6. Minimal APIs — Introduction

We introduced **Minimal APIs** as a lighter alternative to the controller pattern, starting with a standalone `SmallBoy.Api` project and then applying the same ideas to the `Catalog` feature in the main `SoftwareSolution`.

### The Standalone Minimal API (SmallBoy.Api)

```csharp
// WebApplication.CreateSlimBuilder → smallest possible footprint, AOT-friendly
var builder = WebApplication.CreateSlimBuilder(args);

var app = builder.Build();

Todo[] sampleTodos = [ new(1, "Walk the dog"), ... ];

// MapGroup organizes related endpoints under a common prefix
var todosApi = app.MapGroup("/todos");

todosApi.MapGet("/", () => sampleTodos)
        .WithName("GetTodos");

todosApi.MapGet("/{id}", Results<Ok<Todo>, NotFound> (int id) =>
    sampleTodos.FirstOrDefault(a => a.Id == id) is { } todo
        ? TypedResults.Ok(todo)
        : TypedResults.NotFound())
    .WithName("GetTodoById");

app.Run();

public record Todo(int Id, string? Title, DateOnly? DueBy = null, bool IsComplete = false);

// AOT serialization — source-generated, no reflection at runtime
[JsonSerializable(typeof(Todo[]))]
internal partial class AppJsonSerializerContext : JsonSerializerContext { }
```

Key contrasts vs. controllers:
- `CreateSlimBuilder` vs `CreateBuilder` — slimmer startup, better for containers and AOT
- Routes are declared as **delegate lambdas** or static methods, not class methods with attributes
- `Results<Ok<Todo>, NotFound>` is a **typed return type** — the framework knows exactly which responses this endpoint can produce, and the OpenAPI spec reflects that accurately
- `TypedResults.Ok(...)` vs. `Ok(...)` — the typed version carries compile-time type information

### Organizing Minimal API Endpoints by Feature — C# 14 Extension Blocks

For the Catalog feature in `SoftwareSolution`, we used C# 14's new `extension` block syntax to keep the route registration close to the feature code:

```csharp
// src/SoftwareSolution/Software.Api/Catalog/Extensions.cs

public static class Extensions
{
    extension(IEndpointRouteBuilder app)
    {
        public IEndpointRouteBuilder MapCatalogEndpoints()
        {
            var catalogGroup = app.MapGroup("/vendors/{id:guid}/catalog")
                .WithTags("Catalog", "Software")
                .WithDescription("Endpoints for managing software catalogs for vendors.");

            catalogGroup.MapPost("", Add.HandleAsync);
            return app;
        }
    }

    extension(IServiceCollection sp)
    {
        public IServiceCollection UseCatalogServices()
        {
            return sp; // placeholder for future service registrations
        }
    }
}
```

Then in `Program.cs`:
```csharp
builder.Services.UseCatalogServices(); // register catalog services
// ...
app.MapCatalogEndpoints();             // register catalog routes
```

This is a powerful pattern: **each feature folder registers its own routes and services**, and `Program.cs` just calls the feature extensions. As the API grows, `Program.cs` stays clean and each feature is self-contained.

### The Add Catalog Item Handler

```csharp
// src/SoftwareSolution/Software.Api/Catalog/Operations/Add.cs

public static class Add
{
    public static async Task<Results<Created<CatalogDetailsResponse>, NotFound>> HandleAsync(
        Guid id,
        IDocumentSession session,
        CatalogCreateModel catalog)
    {
        // Verify the referenced vendor exists
        var vendorExists = await session.LoadAsync<VendorEntity>(id) != null;
        if (!vendorExists)
        {
            return TypedResults.NotFound();
        }

        var entityToSave = new CatalogEntity
        {
            Id = Guid.NewGuid(),
            VendorId = id,
            Name = catalog.Name
        };
        session.Store(entityToSave);
        await session.SaveChangesAsync();

        var response = new CatalogDetailsResponse
        {
            Id = entityToSave.Id,
            VendorId = entityToSave.VendorId,
            Name = catalog.Name
        };
        return TypedResults.Created($"/vendors/{id}/catalog/{entityToSave.Id}", response);
    }
}
```

Notice:
- The return type `Results<Created<CatalogDetailsResponse>, NotFound>` is self-documenting — a reader can see exactly what this endpoint can return without reading the body
- Minimal API handlers get their dependencies injected as parameters — no constructor, no `[FromServices]` attribute needed
- The guard clause (`if !vendorExists`) returns `404` before touching the database for writes — fail fast and correctly

---

## Key Takeaways

- **Resources are nouns; HTTP methods are verbs.** If your URL has a verb in it, reconsider the design. `POST /hiring-requests` is right; `POST /createEmployee` is not.
- **"Adding a resource" solves most API design problems.** Complex actions (credit updates, termination requests, raise requests) become auditable, navigable resources of their own — not cryptic PATCH payloads.
- **Service lifetimes matter and they are load-bearing.** Mismatching lifetimes (capturing a Scoped service in a Singleton) causes subtle, hard-to-trace bugs. Know the difference: Singleton = forever, Scoped = per-request, Transient = every time.
- **`IHttpContextAccessor` bridges the gap.** When you need HTTP context data (like user identity) inside a service that isn't a controller, `IHttpContextAccessor` is the right tool — but register it Scoped and use `AddHttpContextAccessor()`.
- **Pagination is a first-class concern.** Returning unbounded lists is a performance and reliability hazard. Default `page`/`pageSize` query parameters with a small default size protect both client and server.
- **Sub-resource PUT is often better than full PUT.** `PUT /vendors/{id}/point-of-contact` is more precise, more cacheable, and less prone to partial-update conflicts than `PUT /vendors/{id}` with a full payload.
- **Minimal APIs and controller APIs coexist.** You don't have to pick one for the whole application. Controllers work well for complex, authorization-heavy surfaces; minimal APIs shine for lean, well-defined features. We used both in the same project.
- **Typed results (`Results<T1, T2>`) make your API contract explicit.** The OpenAPI spec reflects the exact possible responses — no more guessing what a handler might return.
- **Feature folders with extension methods scale.** Organizing by feature (`Catalog/Extensions.cs`) rather than by type (`Controllers/CatalogController.cs`) keeps related code together and keeps `Program.cs` from becoming a dumping ground.
- **`Cache-Control` is infrastructure-level configuration.** The right headers transform static or slowly-changing responses into something CDNs and proxies can serve without hitting your server at all.

---

## What's Next — Wrapping Up the Course Arc

Over three days we've built from the ground up: a cloud-native .NET Web API with a real database, authentication, authorization, full CRUD, testing, and now two API styles (controllers and minimal APIs) working side by side. The foundation is solid.

Things worth exploring as you continue:

- **Complete the Catalog API** — add `GET /vendors/{id}/catalog` (list) and `GET /vendors/{id}/catalog/{itemId}` (detail); apply authorization so only Software Center employees can add items
- **Resource-level authorization** — checking not just "is this user a manager?" but "did *this user* create *this vendor*?" — look into ASP.NET Core's `IAuthorizationHandler` and resource-based policies
- **Problem Details (RFC 9457)** — standardize your error responses using `builder.Services.AddProblemDetails()` so clients always get a consistent structure for `400`, `404`, `409`, etc.
- **Output Caching** — `builder.Services.AddOutputCaching()` and `[OutputCache]` on endpoints that return stable data; pairs naturally with the `Cache-Control` concepts from today
- **Async deep dive** — the `async/await` threading model, thread pool starvation under load, and why you should never `await` inside a hot path without understanding what you're awaiting
- **`PATCH` and JSON Merge Patch** — for partial updates where sending the whole sub-resource is too heavy
- **Validation middleware** — `builder.Services.AddValidation()` was added to `Program.cs` today; explore what it enables beyond data annotations (FluentValidation, async validators)
- **Versioning** — how to evolve an API without breaking existing clients; header-based vs URL-based versioning strategies
- **Observability** — the Aspire dashboard you've been running has distributed traces and logs built in; explore what's being captured and how to add custom spans
- **Deployment** — take what you've built and containerize it; the Aspire manifest can generate Docker Compose or Kubernetes manifests directly

The most important thing you leave with is the mental model: HTTP is a rich, constraint-based architecture, and working *with* its constraints — statelessness, resource orientation, uniform interface, cacheability — makes your APIs more reliable, more performant, and more maintainable than fighting against them.
