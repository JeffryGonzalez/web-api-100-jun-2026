using System.ComponentModel.DataAnnotations;

namespace Employees.Api.Employees;

// The INPUT model for creating an employee (sometimes called the "request" or
// "command" model). This is the ONLY shape the client is allowed to send on POST.
//
// Two layers of validation work together here:
//   1. The C# `required` keyword - System.Text.Json refuses to bind the request
//      if any of these members are missing from the JSON body.
//   2. The System.ComponentModel.DataAnnotations attributes - because the
//      controller is marked [ApiController], these are checked automatically and
//      an invalid request returns a 400 with a problem-details body before our
//      action method ever runs.
public record CreateEmployeeModel
{
    [Required, StringLength(100, MinimumLength = 2)]
    public required string FullName { get; init; }

    [Range(0.01, 1_000_000)]
    public required decimal Salary { get; init; }

    // Exactly three letters, e.g. "DEV", "SAL", "QAA", "CEO".
    [RegularExpression("^[A-Za-z]{3}$",
        ErrorMessage = "Department must be exactly three letters.")]
    public required string Department { get; init; }
}
