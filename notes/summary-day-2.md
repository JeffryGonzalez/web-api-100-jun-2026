# Day 2 Summary — Web APIs with .NET (June 9, 2026)

Day 2 took everything from Day 1 and raised the stakes. We started the morning with a hands-on lab (building a fresh `/employees` API from scratch), then spent the afternoon transforming our Vendors API into something you'd actually ship to production: proper response shaping, JWT-based authentication, role-based authorization policies, and a full suite of automated tests. By end of day you had both a working lab solution and a test project that exercises the Vendors API at the HTTP level — spinning up a real database in CI with no external infrastructure.

---

## 1. C# Records Refresher — Immutability and Value Semantics

Before the lab, we did a quick demo (`src/DemoStuff2/`) to reinforce the difference between `record` and `class` in C#, because Day 2 relies on that distinction throughout.

```csharp
// Records have VALUE equality — two records with identical data are "equal"
var cat1 = new Pet { Name = "Bailey" };
var cat2 = new Pet { Name = "Spike", Breed = "Alley Cat" };

// Records are immutable — you can't mutate them, but you CAN copy-and-update with `with`
var updatedSpike = cat2 with { Breed = "Siamese" };

Console.WriteLine(cat1 == cat2);    // False (different data)
Console.WriteLine(cat1.ToString()); // Pet { Name = Bailey, Breed =  } — free!
```

**Why it matters for APIs:** We use `record` types for models (DTOs) — the shapes we receive and return. They are immutable snapshots. We use `class` for entities — things with identity that live in a database and can change state. This is not a style preference; it reflects a real semantic difference in what those objects *are*.

---

## 2. Lab 1 — Building the Employees API (Four Distinct Shapes)

The centerpiece of the morning was **Lab 1** (`labs-jeff/Lab1EmployeesSolution/`): build a `/employees` resource from scratch, with three endpoints — `POST`, `GET /{id}`, and `GET` (list). The key constraint: use **four separate types**, each with a single job.

### The Four-Shape Pattern

| Type | Role | C# Kind | File |
|---|---|---|---|
| `CreateEmployeeModel` | Input the **client sends** on POST. Validated. | `record` | `CreateEmployeeModel.cs` |
| `EmployeeEntity` | What gets **stored in Postgres**. Has server-only fields. | `class` | `EmployeeEntity.cs` |
| `EmployeeModel` | What we **return** from POST and `GET /{id}`. | `record` | `EmployeeModels.cs` |
| `EmployeeSummaryModel` | A **trimmed list view** from `GET /employees`. | `record` | `EmployeeModels.cs` |

The key idea: **the client does not get to decide `Id` or `HiredAt`** — those are server-controlled. So the input type only exposes what a client is allowed to set. What you *store* and what you *return* are separate shapes that can evolve independently.

### Input Validation with Data Annotations

```csharp
public record CreateEmployeeModel
{
    [Required, StringLength(100, MinimumLength = 2)]
    public required string FullName { get; init; }

    [Range(0.01, 1_000_000)]
    public required decimal Salary { get; init; }

    // Exactly three letters: "DEV", "QAA", "SAL", etc.
    [RegularExpression("^[A-Za-z]{3}$",
        ErrorMessage = "Department must be exactly three letters.")]
    public required string Department { get; init; }
}
```

Two layers of protection work together: (1) The C# `required` keyword means `System.Text.Json` rejects requests missing those fields before your code even runs. (2) The data annotation attributes are enforced by `[ApiController]` — a bad request returns a `400 Problem Details` response automatically, with no `if` statements from you.

### The Persistence Entity

```csharp
public class EmployeeEntity
{
    public Guid Id { get; set; }
    public required string FullName { get; init; }
    public required decimal Salary { get; init; }
    public required string Department { get; init; }

    // The client never sends or sees this — it's ours.
    public DateTimeOffset HiredAt { get; set; }

    // Mapping lives HERE — one place decides what "an employee on the wire" looks like.
    public EmployeeModel ToModel() => new()
    {
        Id = Id,
        FullName = FullName,
        Salary = Salary,
        Department = Department,
    };
}
```

### The Controller — All Three Endpoints

```csharp
[ApiController]
public class EmployeesController : ControllerBase
{
    // POST /employees
    [HttpPost("/employees")]
    public async Task<ActionResult<EmployeeModel>> AddEmployeeAsync(
        [FromBody] CreateEmployeeModel request,
        [FromServices] IDocumentSession session)
    {
        var entity = new EmployeeEntity
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName,
            Salary = request.Salary,
            Department = request.Department,
            HiredAt = DateTimeOffset.UtcNow,
        };
        session.Store(entity);
        await session.SaveChangesAsync();
        return Created($"/employees/{entity.Id}", entity.ToModel());
    }

    // GET /employees/{id:guid}
    [HttpGet("/employees/{id:guid}")]
    public async Task<ActionResult<EmployeeModel>> GetEmployeeByIdAsync(
        Guid id, [FromServices] IDocumentSession session)
    {
        var entity = await session.Query<EmployeeEntity>()
            .SingleOrDefaultAsync(e => e.Id == id);
        return entity switch
        {
            null => NotFound(),
            _    => Ok(entity.ToModel())
        };
    }

    // GET /employees
    [HttpGet("/employees")]
    public async Task<ActionResult<IReadOnlyList<EmployeeSummaryModel>>> GetAllEmployeesAsync(
        [FromServices] IDocumentSession session)
    {
        var employees = await session.Query<EmployeeEntity>()
            .Select(e => new EmployeeSummaryModel
            {
                Id = e.Id,
                FullName = e.FullName,
                Department = e.Department,
            })
            .ToListAsync();
        return Ok(employees);
    }
}
```

**Key patterns to notice:**
- The `{id:guid}` route constraint means a non-GUID URL segment naturally returns 404 — the route never matches.
- The list endpoint projects **in the query** — we only pull back the fields we expose. Salary stays in the database.
- `[FromServices]` on action parameters instead of constructor injection — clean and sufficient for simple cases.
- The `entity.ToModel()` call is the only mapping. Change the response shape in one place.

---

## 3. Response Shaping — Never Leak Your Persistence Model

After the lab debrief, we went back to the Vendors API and applied the same principle. Previously the `POST /vendors` endpoint was returning the raw `VendorEntity` — a persistence object with server-internal fields (`CreatedAt`, `CreatedBy`) that clients have no business seeing.

**Before (leaky):**
```csharp
return Created($"/vendors/{entity.Id}", entity); // exposes CreatedAt, CreatedBy
```

**After (shaped):**
```csharp
var response = new VendorDetailsModel
{
    Id = entity.Id,
    Name = entity.Name,
    PointOfContact = entity.PointOfContact,
    Url = entity.Url,
    // CreatedAt and CreatedBy stay hidden
};
return Created($"/vendors/{entity.Id}", response);
```

We also updated `GET /vendors/{id}` to project directly in the Marten query instead of loading the full entity and mapping:

```csharp
var saved = await session.Query<VendorEntity>()
    .Where(v => v.Id == id)
    .Select(v => new VendorDetailsModel
    {
        Id = v.Id,
        Name = v.Name,
        PointOfContact = v.PointOfContact,
        Url = v.Url,
    })
    .SingleOrDefaultAsync();
```

This is better: the projection happens in the database, so only the fields you actually need cross the wire.

---

## 4. Authentication and Authorization — JWT Bearer + Policies

We added authentication and authorization to the Vendors API so that only users with the right roles can create vendors.

### Setting Up JWT Bearer Auth

In `Program.cs`:

```csharp
builder.Services.AddAuthentication().AddJwtBearer(options =>
{
    // Configuration comes from appsettings — ValidAudiences, ValidIssuer
    // The options block is left intentionally blank here; values come from config.
});
```

In `appsettings.Development.json`:
```json
"Authentication": {
  "Schemes": {
    "Bearer": {
      "ValidAudiences": ["https://localhost:9000"],
      "ValidIssuer": "dotnet-user-jwts"
    }
  }
}
```

Using **`dotnet user-jwts`** in development: you can mint your own JWTs from the CLI without a real identity provider, which is perfect for testing authorization logic in isolation.

### Defining an Authorization Policy

```csharp
builder.Services.AddAuthorizationBuilder().AddPolicy("SoftwareCenterManager", policy =>
{
    policy.RequireRole("SoftwareCenter");
    policy.RequireRole("Manager");
    // Both roles are required — not OR, but AND
});
```

### Applying the Policy to an Endpoint

```csharp
[HttpPost("/vendors")]
[Authorize(Policy = "SoftwareCenterManager")]
public async Task<ActionResult<VendorDetailsModel>> AddVendorAsync(...)
```

### Middleware Order Matters

```csharp
app.UseAuthentication(); // 1. Identify who's calling (parse and validate the JWT)
app.UseAuthorization();  // 2. Decide if they're allowed to call this endpoint
app.MapControllers();    // 3. Route to the right action
```

Getting this order wrong is a common mistake. Authentication must happen *before* authorization.

---

## 5. The `ILookupRequestingUsers` Interface — Abstracting Identity

We needed to record *who* created each vendor, but we didn't want the controller to directly call `HttpContext.User`. Instead, we introduced an interface:

```csharp
public interface ILookupRequestingUsers
{
    string GetRequestingUserId();
}
```

Then we injected it into the controller action:

```csharp
public async Task<ActionResult<VendorDetailsModel>> AddVendorAsync(
    [FromBody] VendorCreateModel request,
    [FromServices] IDocumentSession session,
    [FromServices] ILookupRequestingUsers userLookup,  // <-- abstracted identity
    [FromServices] TimeProvider clock                  // <-- abstracted time
)
{
    var entity = new VendorEntity
    {
        ...
        CreatedAt = clock.GetUtcNow(),
        CreatedBy = userLookup.GetRequestingUserId()
    };
}
```

**Why this matters for testing:** In tests, we substitute a fake implementation. The real implementation reads from `HttpContext`; the test implementation returns a hardcoded string. The controller code is identical in both cases — it doesn't know or care which version it has.

Same principle applies to `TimeProvider` (from `Microsoft.Extensions.Time.Testing`): inject the clock rather than calling `DateTime.UtcNow`, and your tests can control time.

---

## 6. Developer Testing — Three Levels and When to Use Each

We covered the testing landscape before building the test suite:

- **Black Box ("outside-in")**: You only have an HTTP client pointing at a running server. Tests exercise the full stack but are slow, fragile, and require the system to be running. Useful for smoke tests and contract verification, not day-to-day development.
- **Unit Tests**: Fast, isolated, test a single class in memory. No database, no HTTP. Good for pure logic (validation rules, business calculations, transformations).
- **System / Integration Tests**: Start the app *in-process* using a real (but controlled) database. You get the full HTTP pipeline, real routing, real auth, real Marten — but without needing an external server running. **This is the sweet spot for API testing.**

---

## 7. System Tests with Alba, Testcontainers, and NSubstitute

The `Software.Tests` project demonstrates the integration testing approach we'll use for the rest of the course.

### The Test Fixture — Starting Everything Once

```csharp
public class VendorsFixture : IAsyncLifetime
{
    public IAlbaHost Host { get; set; } = null!;
    private PostgreSqlContainer _pgContainer = null!;

    public async Task InitializeAsync()
    {
        // Testcontainers spins up a real Postgres 17 Docker container
        _pgContainer = new PostgreSqlBuilder("postgres:17").Build();
        await _pgContainer.StartAsync();

        // Alba boots the actual ASP.NET Core app in-process
        Host = await AlbaHost.For<Program>(config =>
        {
            // Fake the clock — tests get a deterministic DateTimeOffset
            var fakeTimeProvider = new FakeTimeProvider(
                new DateTimeOffset(1969, 4, 20, 23, 59, 59, TimeSpan.FromHours(04)));

            // Fake the user lookup — tests get a known identity
            var fakeUserService = Substitute.For<ILookupRequestingUsers>();
            fakeUserService.GetRequestingUserId().Returns("carl@netscape.com");

            // Override the connection string to point at the container
            config.UseSetting("ConnectionStrings:software", _pgContainer.GetConnectionString());

            config.ConfigureServices(sp =>
            {
                sp.AddScoped(s => fakeUserService);
                sp.AddSingleton<TimeProvider>(s => fakeTimeProvider);
            });
        }, new AuthenticationStub()); // AuthenticationStub lets tests supply claims directly
    }

    public async Task DisposeAsync()
    {
        await Host.DisposeAsync();
        await _pgContainer.DisposeAsync();
    }
}
```

**What `[CollectionDefinition]` + `ICollectionFixture<T>` does:** All test classes in the `"VendorsSystemTests"` collection share *one* instance of `VendorsFixture`. The container and app host start once, all tests run, then everything is torn down. This keeps the test suite fast even as it grows.

### A System Test in Action

```csharp
[Fact]
public async Task AddingAVendor()
{
    var vendorToPost = new VendorCreateModel { Name = "Hypertheory", Url = "...", PointOfContact = ... };

    // POST with valid manager claims → expect 201
    var postResponse = await fixture.Host.Scenario(api =>
    {
        api.WithClaim(new Claim("sub", "jill@company.com"));
        api.WithClaim(new Claim(ClaimTypes.Role, "SoftwareCenter"));
        api.WithClaim(new Claim(ClaimTypes.Role, "Manager"));
        api.Post.Json(vendorToPost).ToUrl("/vendors");
        api.StatusCodeShouldBe(201);
    });

    var postBody = postResponse.ReadAsJson<VendorDetailsModel>();
    Assert.Equal("Hypertheory", postBody.Name);

    // Follow the Location header — GET the resource we just created
    var location = postResponse.Context.Response.Headers.Location.ToString();
    var getResponse = await fixture.Host.Scenario(api =>
    {
        api.Get.Url(location);
        api.StatusCodeShouldBeOk();
    });

    // POST response and GET response are equal (record value equality!)
    var getBody = getResponse.ReadAsJson<VendorDetailsModel>();
    Assert.Equal(postBody, getBody);

    // Go into the database directly and verify server-side fields
    using var sp = fixture.Host.Services.CreateScope();
    using var db = sp.ServiceProvider.GetRequiredService<IDocumentSession>();
    var savedEntity = await db.LoadAsync<VendorEntity>(postBody.Id);
    Assert.Equal("carl@netscape.com", savedEntity.CreatedBy); // from the fake user service
}
```

### Authorization Tests — Testing Rejections

```csharp
[Fact]
public async Task NoAuthGets403()
{
    await fixture.Host.Scenario(api =>
    {
        api.Post.Json(new { }).ToUrl("/vendors");
        api.StatusCodeShouldBe(403); // No claims at all → Forbidden
    });
}

[Fact]
public async Task NonManagerGets403()
{
    await fixture.Host.Scenario(api =>
    {
        api.WithClaim(new Claim("sub", "joe@aol.com"));
        api.WithClaim(new Claim(ClaimTypes.Role, "SoftwareCenter")); // right org, wrong role
        api.Post.Json(new { }).ToUrl("/vendors");
        api.StatusCodeShouldBe(403);
    });
}
```

Testing that the system correctly *rejects* unauthorized requests is just as important as testing that it accepts valid ones.

---

## Key Takeaways

- **Never return your persistence entity through your API.** Always map to a response type. What you store and what you expose are independent concerns — keep them that way from day one.
- **Four shapes per resource** is a useful mental model: input model (validated), entity (stored), write/detail model (single item response), summary model (list response). Not every API needs all four, but know which ones you're collapsing.
- **`required` + data annotations = two-layer validation.** `required` is enforced at bind time; annotations are enforced by `[ApiController]` before your method body runs. Together they eliminate an entire class of defensive `if` statements.
- **Abstract what you can't control in tests.** Time (`TimeProvider`), external identities (`ILookupRequestingUsers`), and any other I/O should come through interfaces. Then tests can substitute fakes without changing the production code at all.
- **System/integration tests with Alba + Testcontainers** give you the full HTTP pipeline and a real database in a test run, with no external server required. This is the testing level that gives you the most confidence per unit of effort for API work.
- **Middleware order is load-bearing.** `UseAuthentication()` must come before `UseAuthorization()`, which must come before `MapControllers()`. The pipeline is sequential; getting this wrong silently breaks your auth.
- **Record value equality is a superpower in tests.** When your response types are records, `Assert.Equal(postBody, getBody)` compares all properties structurally. No manual field-by-field comparison needed.
- **`[CollectionFixture]` keeps integration test suites fast.** Spin up one database container and one app host for all tests in a collection. Don't pay the startup cost for every test class.

---

## What's Next (Day 3)

Building on today's foundation, Day 3 will likely explore:

- **`PUT` and `DELETE` endpoints** — completing the CRUD surface for Vendors. `204 No Content` for successful updates, idempotency considerations.
- **Returning lists with shaping** — `GET /vendors` with pagination, filtering, or sorting; thinking about collection responses vs. single-item responses.
- **More complex authorization** — resource-level authorization (can this user modify *this specific* vendor?), not just role checks.
- **Error handling and Problem Details** — standardizing how validation errors, not-found cases, and server errors are communicated to clients.
- **Additional resources** — beginning work on Catalog Items and nested routes like `POST /vendors/{vendorId}/catalog-items`.
- **Expanding the test suite** — adding tests for the new endpoints and edge cases using the patterns established today.
