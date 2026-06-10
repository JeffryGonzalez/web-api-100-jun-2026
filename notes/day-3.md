# Good Morning! It's June 10, 2026 **DAY 3**

## Getting in to your virtual machines

https://class.hypertheory-labs.com/

Let me know if you have any issues.

Once you are in, please start Docker Desktop.


## RIGHT AFTER LUNCH

- Additional Stuff
    - Getting your code up to Github
- Show `GET /vendors` - pagination
- Design DELETE
- Design PUT

- Catalog Items

## Today

- Day 2 Review
- Q&A
- Providing Services
  - Service Lifetime
- async/await - threading, thread pool starvation
- Vendor List
- Updating a Resource
- Removing a Resource

- Catalog Items
- Intro to Controller-less (minimal) APIs
- API Design
- Final Lab
  - Complete the Catalog API



## Example of why you don't use the persistence model outside of the API

```http
GET https://hr.company.com/api/employees/3983989389
Accept: application/json 
```

```csharp
public class EmployeeEntity
{
    public Guid Id { get; set; }
    public required string FullName { get; init; }
    public required decimal Salary { get; init; }
    public required string Department { get; init; }
    public bool ScheduledForLayoff {get; init; }

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
[Authorize]
GET /employees/938938983
```http
200 OK
Content-Type: application/json

{
  "id": "3983989389",
  "fullName": "Sue Jones",
  "department": "DEV"
}
```

[Authorize(Policy="Managers")]
GET /employees/{id}/pay-info

```http
200 OK
Content-Type: application/json

{
  "current": 80000,
  "type": "Salaried",
  "payPeriod": "Monthly"
}
```


### Vendors
GET /vendors/{id}
```json
{
    "id": "9398398938",
    "name": "Netscape",
    "url": "https://www.microsoft.com",
    "pointOfContact": {
        "name": "Satya Nadella",
        "email": "Satya@microsoft.com",
        "phone": "999-999-9999"
    }
}
```

DML
SELECT  GET
INSERT  POST
DELETE  DELETE
UPDATE  
        PUT - replace the entity at this URL with this new entity.



"Miniput"

PUT /vendors/{id}/point-of-contact

```
{
 
        "name": "Scott Hanselmann",
        "email": "scott@microsoft.com",
        "phone": "999-999-9999"
    
}
```