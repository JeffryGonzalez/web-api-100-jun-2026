namespace Employees.Api.Employees;

// The OUTPUT models - the shapes we hand back to clients. These are records
// because they are immutable, value-like snapshots of data. They are deliberately
// kept separate from the entity so that what we STORE and what we EXPOSE can
// evolve independently (e.g. the entity has HiredAt; these do not).

// The "write model": what we return after a successful write (POST) and from a
// single-item read (GET /employees/{id}). It is a full view of the employee
// EXCEPT for server-internal fields like HiredAt.
public record EmployeeModel
{
    public required Guid Id { get; init; }
    public required string FullName { get; init; }
    public required decimal Salary { get; init; }
    public required string Department { get; init; }
}

// A trimmed "read model" for the collection (GET /employees). The list omits
// salary and everything else - just enough to identify each employee.
public record EmployeeSummaryModel
{
    public required Guid Id { get; init; }
    public required string FullName { get; init; }
    public required string Department { get; init; }
}
