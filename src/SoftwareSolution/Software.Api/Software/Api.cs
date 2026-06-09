using JasperFx.Core;
using Marten;
using Microsoft.AspNetCore.Authorization;
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
    [Authorize(Policy = "SoftwareCenterManager")]
    public async Task<ActionResult<VendorEntity>> AddVendorAsync(
        [FromBody] VendorCreateModel request,
        [FromServices] ILogger<Api> logger, 
        [FromServices] IDocumentSession session,
        [FromServices] ILookupRequestingUsers userLookup,
        [FromServices] TimeProvider clock
        )
    {
        // Backing Service - database, cache, message broker, etc. NEVER USE THE NEW
        var entity = new VendorEntity
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Url = request.Url,
            PointOfContact = request.PointOfContact,
            CreatedAt = clock.GetUtcNow(),
            CreatedBy = userLookup.GetRequestingUserId()
        };
        logger.LogInformation("Just added a new vendor {vendor}", entity.Name);
        session.Store(entity);
        await session.SaveChangesAsync();
        var response = new VendorDetailsModel { 
           Id = entity.Id,
           Name = entity.Name,
           PointOfContact = entity.PointOfContact,
           Url = entity.Url,

        };
        return Created($"/vendors/{entity.Id}", response);
    }

    // Route Parameter - a "segment" of the URL that holds data.

    [HttpGet("/vendors/{id:guid}")]
    public async Task<ActionResult<VendorDetailsModel>> GetByIdAsync(Guid id, [FromServices] IDocumentSession session)
    {
        var saved = await session.Query<VendorEntity>()
            .Where(v => v.Id == id)
            .Select(v => new VendorDetailsModel
            {
                Name = v.Name,
                PointOfContact = v.PointOfContact,
                Url = v.Url,
                Id= v.Id
            })
            
            .SingleOrDefaultAsync();

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
    public string CreatedBy { get; set; } = string.Empty;
}

public record VendorDetailsModel
{
    public Guid Id { get; set; }
    public required string Name { get; init; }
    public required string Url { get; init; }
    public required VendorPointOfContact PointOfContact { get; init; }
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