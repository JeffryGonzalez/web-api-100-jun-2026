namespace Employees.Api.Employees;

// The PERSISTENCE model (the "entity"). This is the shape we store in the database.
// It is a class (not a record) because it represents a mutable, identity-bearing
// thing that lives in storage. It holds fields the outside world should NOT see,
// like the HiredAt timestamp we stamp on the server.
public class EmployeeEntity
{
    public Guid Id { get; set; }
    public required string FullName { get; init; }
    public required decimal Salary { get; init; }
    public required string Department { get; init; }

    // Server-controlled. The client never sends this and never sees it.
    public DateTimeOffset HiredAt { get; set; }

    // Map the entity to the model we hand back to clients.
    // Keeping the mapping here means there is exactly ONE place that decides
    // what an employee "looks like" on the wire (note: no HiredAt).
    public EmployeeModel ToModel() => new()
    {
        Id = Id,
        FullName = FullName,
        Salary = Salary,
        Department = Department,
    };
}
