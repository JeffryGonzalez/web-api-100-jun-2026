using Marten;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Software.Api.Software;

[ApiController]
// This "ApiController" says "validate the request model before calling the method"
public class Api : ControllerBase
{
    //private IDocumentSession _documentSession;

    //public Api(IDocumentSession documentSession)
    //{
    //    _documentSession = documentSession;
    //}

    [HttpPost("/vendors")]
    
    public async Task<ActionResult<VendorEntity>> AddVendorAsync(
        [FromBody] VendorCreateModel request,
        [FromServices] IDocumentSession session)
    {
        // Backing Service - database, cache, message broker, etc. NEVER USE THE NEW
        var entity = new VendorEntity
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Url = request.Url,
            PointOfContact = request.PointOfContact,
            CreatedAt = DateTime.UtcNow,
        };
        session.Store(entity);
        await session.SaveChangesAsync();
        return Created($"/vendors/{entity.Id}", entity);
    }

    // Route Parameter - a "segment" of the URL that holds data.

    [HttpGet("/vendors/{id:guid}")]
    public async Task<ActionResult<VendorEntity>> GetByIdAsync(Guid id, [FromServices] IDocumentSession session)
    {
        var saved = await session.Query<VendorEntity>().SingleOrDefaultAsync(v => v.Id == id);

        return saved switch
        {
            null => NotFound(), // 404
            _ => Ok(saved) // 200 - with the data.
        };
        
    }
}


public class VendorEntity
{
    public Guid Id { get; set; }
    public required string Name { get; init; }
    public required string Url { get; init; }
    public required VendorPointOfContact PointOfContact { get; init; }
    public DateTimeOffset CreatedAt { get; set; }
}

public record VendorPointOfContact
{
    public required string Name { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Phone { get; init;  } = string.Empty;
}

public record VendorCreateModel
{
    [MinLength(5), MaxLength(100)]
    public required string Name { get; init;  }
    [Url]
    public required string Url { get; init; }
    public required VendorPointOfContact PointOfContact { get; init; } 
}