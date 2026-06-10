using Marten;
using Microsoft.AspNetCore.Http.HttpResults;
using Software.Api.Vendors;

namespace Software.Api.Catalog.Operations;

public static class Add
{
    public static async Task<Results<Created<CatalogDetailsResponse>, NotFound>> HandleAsync(Guid id, IDocumentSession session, CatalogCreateModel catalog)
    {
        // make sure the vendor exists 
        var vendorExists = await session.LoadAsync<VendorEntity>(id) != null;
        if (vendorExists == false)
        {
            return TypedResults.NotFound();
        }
        var entityToSave = new CatalogEntity
        {
            Id = Guid.NewGuid(),
            VendorId = id,
            Name = catalog.Name
        };
        session.Store(entityToSave);
        await session.SaveChangesAsync();

        var response = new CatalogDetailsResponse
        {
            Id = entityToSave.Id,
            VendorId = entityToSave.VendorId,
            Name = catalog.Name
        };
        return TypedResults.Created($"/vendors/{id}/catalog/{entityToSave.Id}", response);
    }   

}
