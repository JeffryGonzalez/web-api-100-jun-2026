# Employees API Lab — Detailed Walkthrough

This is the step-by-step version of [`overview.md`](./overview.md). If you're
comfortable, work straight from the overview and only dip in here when you get
stuck. Otherwise, follow along start to finish.

The goal is to build one resource, `/employees`, with three endpoints, while
practicing a cleaner separation of types than we used yesterday.

## The big idea: four kinds of "shape"

Yesterday (in the Software solution) we returned the database entity directly
from our endpoints. That works, but it blurs together things that should be
separate. Today we'll use **four distinct types**, each with one job:

| Type | Role | C# kind | File |
| --- | --- | --- | --- |
| `CreateEmployeeModel` | The **input** a client sends to POST. Validated. | `record` | `CreateEmployeeModel.cs` |
| `EmployeeEntity` | The **persistence** shape we store in Postgres. Has server-only fields. | `class` | `EmployeeEntity.cs` |
| `EmployeeModel` | The **write model** we return after a write and from a single-item read. | `record` | `EmployeeModels.cs` |
| `EmployeeSummaryModel` | The trimmed **read model** for the list. | `record` | `EmployeeModels.cs` |

Why bother?

- The client should not be able to send us an `Id` or a `HiredAt` — those belong
  to the server. So the **input** type only has the fields a client is allowed to set.
- The thing we **store** has fields we don't want to leak (today that's `HiredAt`).
- So the thing we **return** is its own shape, mapped from the entity.

> We're deliberately reusing one `EmployeeModel` for both the POST response and the
> GET-by-id response. We'll talk after the lab about when that's fine and when it
> bites you — the same question comes up in the Software API.

Records are used for the models because they're immutable, value-like snapshots.
The entity is a `class` because it's a mutable, identity-bearing thing that lives
in the database.

## Before you start

1. Set **AppHost** as the startup project and run it (Aspire spins up Postgres
   and the API). The Aspire dashboard opens; from there you can reach the API.
2. The database is already wired up: `AppHost.cs` creates a Postgres resource
   named `employees` and hands it to the API, and Marten is configured in
   `Program.cs`. You don't need to write any data-access plumbing.

> **Heads up — connection name:** the data source name in `Program.cs`
> (`AddNpgsqlDataSource(connectionName: "employees")`) must match the database
> resource name in `AppHost.cs` (`pg.AddDatabase("employees")`). If they don't
> match, the API throws "no connection string" at startup.

We'll put all of our new code in an `Employees/` folder inside the
`Employees.Api` project, so create that folder first.

## Step 1 — The persistence entity

Create `Employees/EmployeeEntity.cs`. This is what we store.

```csharp
namespace Employees.Api.Employees;

public class EmployeeEntity
{
    public Guid Id { get; set; }
    public required string FullName { get; init; }
    public required decimal Salary { get; init; }
    public required string Department { get; init; }

    // Server-controlled. The client never sends or sees this.
    public DateTimeOffset HiredAt { get; set; }
}
```

Notes:

- `required` (the C# keyword) means you can't construct an `EmployeeEntity`
  without setting these members — the compiler enforces it.
- `Id` and `HiredAt` are set by *us*, not the client.

We'll add a mapping method to this class in Step 4.

## Step 2 — The input model (with validation)

Create `Employees/CreateEmployeeModel.cs`. This is the only shape a client may POST.

```csharp
using System.ComponentModel.DataAnnotations;

namespace Employees.Api.Employees;

public record CreateEmployeeModel
{
    [Required, StringLength(100, MinimumLength = 2)]
    public required string FullName { get; init; }

    [Range(0.01, 1_000_000)]
    public required decimal Salary { get; init; }

    [RegularExpression("^[A-Za-z]{3}$",
        ErrorMessage = "Department must be exactly three letters.")]
    public required string Department { get; init; }
}
```

There are **two layers of validation** working together here, and it's worth
understanding the difference:

1. **The `required` keyword** — enforced by the JSON deserializer. If the client
   leaves `salary` out of the body entirely, the request is rejected before any
   attribute is even considered. Try it: POST a body with no `salary`.
2. **The data-annotation attributes** (`[Range]`, `[RegularExpression]`, etc.) —
   these check the *values* once they're bound. `"DEVELOPMENT"` is present but
   fails the three-letter rule.

The attributes only run automatically because our controller is marked
`[ApiController]` (next step). When validation fails, the framework returns a
`400` with a `ProblemDetails` body listing the errors — you don't write any of
that yourself.

## Step 3 — The output models

Create `Employees/EmployeeModels.cs`. These are what we hand back.

```csharp
namespace Employees.Api.Employees;

// The "write model": returned from POST and from GET /employees/{id}.
// A full view EXCEPT for server-internal fields like HiredAt.
public record EmployeeModel
{
    public required Guid Id { get; init; }
    public required string FullName { get; init; }
    public required decimal Salary { get; init; }
    public required string Department { get; init; }
}

// A trimmed read model for the collection. No salary.
public record EmployeeSummaryModel
{
    public required Guid Id { get; init; }
    public required string FullName { get; init; }
    public required string Department { get; init; }
}
```

Notice `EmployeeModel` has no `HiredAt`. That's the whole point of having a
separate type — we choose exactly what leaves the building.

## Step 4 — Map the entity to the write model

Back in `EmployeeEntity.cs`, add a method so there's **one place** that decides
what an employee looks like on the wire:

```csharp
public EmployeeModel ToModel() => new()
{
    Id = Id,
    FullName = FullName,
    Salary = Salary,
    Department = Department,
};
```

## Step 5 — The controller

Create `Employees/EmployeesController.cs` with all three endpoints.

```csharp
using Marten;
using Microsoft.AspNetCore.Mvc;

namespace Employees.Api.Employees;

[ApiController]
public class EmployeesController : ControllerBase
{
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

        var response = entity.ToModel();
        return Created($"/employees/{entity.Id}", response);
    }

    [HttpGet("/employees/{id:guid}")]
    public async Task<ActionResult<EmployeeModel>> GetEmployeeByIdAsync(
        Guid id,
        [FromServices] IDocumentSession session)
    {
        var entity = await session.Query<EmployeeEntity>()
            .SingleOrDefaultAsync(e => e.Id == id);

        return entity switch
        {
            null => NotFound(),
            _ => Ok(entity.ToModel()),
        };
    }

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

Things to call out:

- **`Created(...)`** returns `201` *and* sets the `Location` response header to the
  URL of the new employee. Check for it in your HTTP client's response headers.
- **The `{id:guid}` route constraint** does double duty: it binds `id` as a `Guid`,
  and if the value in the URL *isn't* a well-formed Guid, this route simply doesn't
  match — so a bad id falls through to a `404` for free. No manual parsing.
- **We project in the list query** (`.Select(...)`) so the summary endpoint only
  pulls back the three fields it exposes.
- **We inject `IDocumentSession` per-action** with `[FromServices]`. You could
  inject it once via the constructor instead — both are fine; we'll discuss the
  trade-offs.

## Step 6 — Try it

Use `Employees.Api/Employees.Api.http` (already filled in with sample requests),
or the Aspire dashboard, or curl. Walk through:

1. **POST a valid employee** → `201`, a `Location` header, and a body with no
   `hiredAt`. Copy the `id` from the response.
2. **POST an invalid department** (`"DEVELOPMENT"`) → `400` with a validation message.
3. **POST with `salary` missing entirely** → `400` from the `required` keyword.
4. **GET `/employees/{that-id}`** → `200`, same shape as the POST response.
5. **GET `/employees/not-a-guid`** → `404` (route constraint never matched).
6. **GET `/employees`** → a list of `{ id, fullName, department }` — no salary.

## Stretch goals (if you finish early)

- Normalize `Department` to upper-case before storing (so `"dev"` is saved as `"DEV"`).
- Add a custom validation message for `FullName` and confirm it shows up in the `400`.
- Add a `400` test for a salary of `0` and confirm `[Range]` catches it.
- Think about: is reusing `EmployeeModel` for both the POST response and the
  GET-by-id response a good idea? What would change if the list needed paging?
  (We'll discuss.)

## Prompts for further learning

The AI in your editor is at least as useful for *understanding* this code as for
*writing* it — arguably more. The trick is to aim it at your mental model, not
just the file. See [`notes/ai-learning.md`](../../notes/ai-learning.md) for
the general patterns; below are ones wired to *this* lab. Each is best run with
the relevant file open so the AI is reacting to your real code, not a hypothetical.

**Build the model**
- "In this lab the DTOs are `record`s but `EmployeeEntity` is a `class`. Explain
  why. Then: what would actually go wrong if I flipped them?"
- "I think the `required` keyword and the `[Required]` attribute do the same job.
  Am I right? Where does my mental model break down?" *(They don't — one is the
  compiler, one is the deserializer, one is validation. Make the AI separate them.)*

**Learn from failure**
- Break the connection name in `Program.cs` on purpose, run it, read the crash.
  Then: "Why did *this specific* error happen at startup, and what mental model of
  how Aspire hands a connection string to Marten would have predicted it?"
- "Show me three ways a request could get past my validation and store bad data
  anyway. For each, show the exact body that does it." *(The fix is cheap; the
  failure intuition is the asset.)*

**Go deeper on what you're standing on**
- "I called `session.Query<EmployeeEntity>()` and never wrote SQL or a migration.
  What is Marten actually doing to my Postgres database? Show me the table and how
  my entity is stored." Then go look in the Aspire dashboard.
- "What does `[ApiController]` turn on that I'm getting for free here? List each
  behavior and which line of my code depends on it."

**Challenge the design (and your own taste)**
- Jeff hinted that reusing one `EmployeeModel` for both POST and GET-by-id is a
  smell. Before he tells you why: "Argue the strongest case *for* a single shared
  model, then the strongest case *against*. Which holds up, and when does the
  answer flip?" Form your own view first.
- "Here's how I'd have written the list endpoint without projection
  *(paste yours)* — what are the advantages of the `.Select(...)` approach, or the
  disadvantages of mine, that I should consider?"

**Then put the AI in the passenger seat**
- Write the `PUT /employees/{id}` (update) or `DELETE` endpoint *yourself* first.
  Only then: "Review my endpoint — what's missing, what would a senior dev flag,
  and why?" You stay the author; the AI is the reviewer.

> The point isn't to have the AI hand you the next answer — it's to use it to find
> the *edges* of what you understand and push on them. That's the difference
> between a tool that makes you faster and one that makes you better. (Yes, this is
> a prompt, prompting you, to prompt an AI. Strange world. Enjoy it.)

> Reminder: this is intentionally *not* how you'd model real hiring — `Salary` as
> a required field on creation, a one-model-fits-all response, etc. We're
> practicing organization and validation. We'll critique the design together
> afterward, and you'll see the same questions resurface in the Software API.
