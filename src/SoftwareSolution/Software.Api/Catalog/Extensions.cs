using Marten;
using Software.Api.Catalog.Operations;
using Software.Api.Vendors;
using System.ComponentModel.DataAnnotations;

namespace Software.Api.Catalog;

public static class Extensions
{
    //public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    //{
     
    //    return app;
    //}

    extension(IEndpointRouteBuilder app)
    {
        
        public IEndpointRouteBuilder MapCatalogEndpoints()
        {
            // POST for /vendors/{id}/catalog
            var catalogGroup = app.MapGroup("/vendors/{id:guid}/catalog").WithTags("Catalog", "Software")
                .WithDescription("Endpoints for managing software catalogs for vendors.");
               
            catalogGroup.MapPost("", Add.HandleAsync).WithDescription("").WithDescription("Do It");
            return app;
        }

       
    }

    extension(IServiceCollection sp)
    {
        public IServiceCollection UseCatalogServices()
        {
            return sp;
        }
    }
}


public record CatalogCreateModel
{
    [MaxLength(100)]
    public required string Name { get; init; }
}

public class CatalogEntity
{
    public Guid Id { get; init; }
    public Guid VendorId { get; init; }
    public required string Name { get; init; }

}


public record CatalogDetailsResponse
{
    public Guid Id { get; init; }
    public Guid VendorId { get; init; }
    public required string Name { get; init; }

}