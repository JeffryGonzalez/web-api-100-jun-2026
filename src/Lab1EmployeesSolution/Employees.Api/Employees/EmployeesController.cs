using Marten;
using Microsoft.AspNetCore.Mvc;

namespace Employees.Api.Employees;

[ApiController]
// [ApiController] gives us automatic model validation: if the bound
// CreateEmployeeModel fails any of its data-annotation rules, the framework
// short-circuits with a 400 before our method body runs.
public class EmployeesController : ControllerBase
{
    // POST /employees - add a new employee.
    [HttpPost("/employees")]
    public async Task<ActionResult<EmployeeModel>> AddEmployeeAsync(
        [FromBody] CreateEmployeeModel request,
        [FromServices] IDocumentSession session)
    {
        // Turn the validated input into the entity we persist. The server owns
        // the Id and the HiredAt timestamp - the client does not provide them.
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

        // 201 Created, a Location header pointing at the new resource, and the
        // write model (no HiredAt) in the body.
        var response = entity.ToModel();
        return Created($"/employees/{entity.Id}", response);
    }

    // GET /employees/{id} - retrieve a single employee.
    // The {id:guid} route constraint means a non-Guid value never matches this
    // route, so a malformed id naturally returns a 404.
    [HttpGet("/employees/{id:guid}")]
    public async Task<ActionResult<EmployeeModel>> GetEmployeeByIdAsync(
        Guid id,
        [FromServices] IDocumentSession session)
    {
        var entity = await session.Query<EmployeeEntity>()
            .SingleOrDefaultAsync(e => e.Id == id);

        return entity switch
        {
            null => NotFound(),              // 404 - not in the database
            _ => Ok(entity.ToModel()),       // 200 - same shape as the POST response
        };
    }

    // GET /employees - list all employees, trimmed to the summary read model.
    [HttpGet("/employees")]
    public async Task<ActionResult<IReadOnlyList<EmployeeSummaryModel>>> GetAllEmployeesAsync(
        [FromServices] IDocumentSession session)
    {
        // Project in the query so we only pull back the fields we expose.
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
