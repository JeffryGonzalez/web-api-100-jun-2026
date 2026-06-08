using Microsoft.AspNetCore.Mvc;

namespace Software.Api.Software;

public class Api : ControllerBase
{
    [HttpPost("/vendors")]
    public async Task<ActionResult> AddVendorAsync([FromBody] VendorCreateModel request)
    {
        return StatusCode(201, request);
    }
}



public record VendorPointOfContact
{
    public required string Name { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Phone { get; init;  } = string.Empty;
}

public record VendorCreateModel
{
    public required string Name { get; init;  }
    public required string Url { get; init; }
    public required VendorPointOfContact PointOfContact { get; init; } 
}