# Day 1 Summary — Web APIs with .NET (June 8, 2026)

Day 1 was all about orientation: understanding *why* we build Web APIs the way we do, and getting hands-on with the full stack of tools we'll use for the rest of the course. We moved quickly from the smallest possible .NET web app up to a real multi-project solution with a database, an API framework, and an orchestration layer — by the end of the day you had a working `POST /vendors` endpoint that actually persists data to PostgreSQL.

---

## 1. Course Philosophy and Cloud-Native Mindset

Before writing any code, we grounded ourselves in some guiding principles:

- **"Cloud Native"** means writing code that can run anywhere — on-prem, AWS, Azure, your laptop — without being tied to a specific platform. This is your *default stance*; exceptions are allowed, but they need to be deliberate.
- **DevOps payoff**: the goal is for teams to independently deploy new versions of their software *whenever they need to*, without coordinating with other teams. Everything we build this week serves that goal.
- The instructor was candid: what's shown here may not match what your team currently does, but don't dismiss it — your team may be using pre-DevOps habits worth revisiting.

---

## 2. HTTP and REST Foundations

We reviewed the conceptual foundation of everything we are building:

- **HTTP** is defined by 7 constraints; the key ones for us are *request/response* and *stateless message passing*.
- **REST** (Representational State Transfer) is an architectural style that maps cleanly onto HTTP. When people say "REST API," they mean an API that uses HTTP's verbs, URLs, and status codes intentionally.
- We covered the core HTTP methods — `GET`, `POST`, `PUT`, `DELETE` — and what each one semantically promises to the caller.

---

## 3. Your First .NET Web API — Three Ways

We built the same basic idea three different ways to show the spectrum from minimal to fully structured.

### The One-File Script (src/demo1/api.cs)

Using .NET 10's new script-style syntax (no project file needed!), the smallest possible API:

```csharp
#:sdk Microsoft.NET.Sdk.Web

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/message", () => "Groovy");

app.Run();
```

**Why it matters:** This shows the essential structure every ASP.NET Core app shares — *build phase* (services/config), then *run phase* (middleware/routes). Every more complex example is an elaboration of this pattern.

### The Default Template (src/First/Program.cs)

The standard `dotnet new webapi` starting point — the familiar weather forecast example. This gave us a reference point for what scaffolding generates and what to customize.

### The Controller Project (src/SoftwareSolution/Software.Api/)

The production-style approach we will use for the rest of the course:

```csharp
builder.Services.AddControllers();
// ...
app.MapControllers(); // Uses reflection to wire up routes from controller attributes
```

Key insight from the code comments in `Program.cs`: everything *above* `builder.Build()` is **service/host configuration**; everything *after* is **middleware/pipeline configuration**. You cannot call `builder.Services.Add...` after building the app.

---

## 4. Controller-Based APIs and the [ApiController] Attribute

We built a `StatusController` to understand the basic structure:

```csharp
public class StatusController : ControllerBase
{
    [HttpGet("/status")]
    public async Task<ActionResult> GetTheStatus()
    {
        var response = new StatusResponseMessage
        {
            Status = "Awesome, all system go!",
            Checked = DateTimeOffset.UtcNow,
        };
        return Ok(response);
    }
}
```

Then we moved to `Software.Api/Software/Api.cs`, the core of our class project. The `[ApiController]` attribute enables **automatic model validation**: if the incoming request body fails validation, the framework returns a `400 Bad Request` *before your method even runs*.

**Key patterns shown:**
- `ControllerBase` (not `Controller` — we do not need view support)
- `[HttpPost("/vendors")]` and `[HttpGet("/vendors/{id:guid}")]` route attributes
- `[FromBody]` for request body binding
- `[FromServices]` for injecting dependencies directly into action parameters (an alternative to constructor injection)
- Return types: `ActionResult<T>`, `Ok(...)`, `Created(...)`, `NotFound()`, `StatusCode(201, ...)`

---

## 5. The Application Domain: Software Center API

All hands-on work this week is built around a real scenario: a **Software Center API** for tracking vendors and approved software catalog items. The full requirements are in `docs/software-center.md`. Day 1 focused on the **Vendors** resource.

The Vendor data model:

| Field | Notes |
|---|---|
| `id` | GUID, assigned by the system at creation time |
| `name` | Required, 5 to 100 characters |
| `url` | Required, must be a valid URL |
| `pointOfContact` | Name, email, phone |
| `createdAt` | UTC timestamp |

We built two endpoints by end of day:
- `POST /vendors` — creates a vendor, persists to the database, returns `201 Created`
- `GET /vendors/{id}` — retrieves a vendor by GUID, returns `404` if not found

---

## 6. Marten and PostgreSQL for Persistence

Rather than Entity Framework, we are using **Marten** — a document/event store that runs on top of PostgreSQL. This lets us think about domain objects naturally without ORM mapping ceremony.

**Setup in Program.cs:**

```csharp
var connectionString = builder.Configuration.GetConnectionString("software")
    ?? throw new Exception("No Connection String");

builder.Services.AddMarten(options =>
{
    options.Connection(connectionString);
}).UseLightweightSessions();
```

**Usage in the controller:**

```csharp
// POST /vendors
session.Store(entity);
await session.SaveChangesAsync();
return Created($"/vendors/{entity.Id}", entity);

// GET /vendors/{id}
var saved = await session.Query<VendorEntity>()
    .SingleOrDefaultAsync(v => v.Id == id);

return saved switch
{
    null => NotFound(),
    _ => Ok(saved)
};
```

Notice the **C# pattern match** on the return — if null, 404; otherwise, 200 with data. Clean and expressive.

**Important principle shown in the code comments:** backing services (database, cache, message broker) are *never* instantiated with `new`. They come in via dependency injection. This is what makes code environment-portable and testable.

---

## 7. .NET Aspire — Local Orchestration

Getting Postgres running locally used to mean installing it manually and hoping your dev environment matched production. With **.NET Aspire**, the `AppHost` project manages all of that:

```csharp
// AppHost/AppHost.cs
var pgServer = builder.AddPostgres("postgres")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithImageTag("17"); // match your production version — dev/prod parity

var softwareDb = pgServer.AddDatabase("software");

var softwareApi = builder.AddProject<Projects.Software_Api>("software-api")
    .WithReference(softwareDb)
    .WaitFor(softwareDb);
```

What this buys you:
- Postgres 17 container spins up automatically via Docker
- Correct connection string injected into the API project — no manual config
- The API waits for the database to be ready before starting
- `ContainerLifetime.Persistent` means the DB container survives restarts, preserving dev data

We also integrated **Scalar** for a modern API explorer UI, and added **Service Defaults** — a shared project that wires up health checks and observability with two lines:

```csharp
builder.AddServiceDefaults();   // in Program.cs
app.MapDefaultEndpoints();      // health check endpoints: /health, /alive
```

---

## 8. OpenAPI and API Documentation

ASP.NET Core generates an OpenAPI spec automatically from your controllers and models:

```csharp
builder.Services.AddOpenApi();
// ...
app.MapOpenApi(); // Serves GET /openapi/v1.json
```

The `DemoOpenApiSolution` project shows how to add **Swagger UI** on top:

```csharp
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "v1");
});
```

For the main `SoftwareSolution`, we use **Scalar** (wired through Aspire) instead of Swagger UI — it's a more modern alternative. The generated `DemoOpenApi.json` file in the repo is a build artifact showing exactly what schema the framework infers from your C# types.

---

## Key Takeaways

- **Every ASP.NET Core app follows the same two-phase pattern**: configure services, then configure middleware. Internalize this and everything else clicks.
- **Cloud Native means portable code**: no hardcoded infrastructure, inject everything, run anywhere.
- **`[ApiController]` handles validation automatically** — you get `400 Bad Request` for bad input without writing a single `if` statement.
- **Aspire replaces "just install Postgres on your laptop"** — the AppHost *is* your local environment spec. New team member? Clone the repo, run the AppHost, everything starts.
- **Marten lets you think in documents** — store a C# object, query it back. No mapping ceremony.
- **Never `new` a backing service** — always inject it. This is what makes code testable and portable.
- **HTTP verbs have semantic meaning** — `GET` is safe and idempotent (cacheable); `POST` creates something new. Use them correctly and HTTP infrastructure works *with* you.
- **Data annotations + `[ApiController]`** = free request validation: `[MinLength(5)]`, `[MaxLength(100)]`, `[Url]` on your request models and you get proper error responses automatically.

---

## What's Next (Day 2)

Day 2 will build on today's foundation:

- **`GET /vendors`** — returning a list and shaping the response (you probably don't want to expose every field to every caller)
- **`PUT /vendors/{id}/point-of-contact`** — updating a sub-resource; `204 No Content` responses
- **HTTP status codes in depth** — when to use `201` vs `200`, `400` vs `422`, `404` vs `409`
- **Response shaping** — separating your internal `VendorEntity` from the `VendorResponse` you expose to callers (never leak your persistence model through your API contract)
- Beginning work on **Catalog Items** and nested routes like `POST /vendors/{vendorId}/catalog-items`
