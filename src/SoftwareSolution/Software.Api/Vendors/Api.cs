using Marten;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Software.Api.Software;

namespace Software.Api.Vendors;

[ApiController]
// This "ApiController" says "validate the request model before calling the method"
public class Api : ControllerBase
{

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
        // We know the person is logged in, a manager, in the software center
        // and the data they passed is "good"
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

    [HttpGet("/vendors")]
    public async Task<ActionResult> GetVendorsAsync([FromServices] IDocumentSession session,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        CancellationToken token = default)
    {
        var results = await session.Query<VendorEntity>()
            .OrderBy(v => v.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => new VendorSummaryModel()
            {
                Id = v.Id,
                Name = v.Name,
                Url = v.Url
            })
            .ToListAsync(token);
        
        var response = new VendorSummary
        {
            Vendors = results,
            Page = page,
            PageSize = pageSize
        };
        return Ok(response);
    }

    [HttpDelete("/vendors/{id:guid}")]
    public async Task<ActionResult> DeleteAsync(Guid id, [FromServices] IDocumentSession session)
    {
        // only the person that created it can delete it - otherwise 403,
        // can't delete if it has catalog items.
        return NoContent();
    }

    [HttpPut("/vendors/{id:guid}/point-of-contact")]
    public async Task<ActionResult> ReplacePointOfContactAsync(Guid id, [FromServices] IDocumentSession session,
        [FromBody] VendorPointOfContact request)
    {
        // if the vendor does not exist, return 404
        // 
        return NoContent();
    }
}

