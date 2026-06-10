using Alba;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Software.Api.Software;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using Software.Api.Vendors;

namespace Software.Tests.Vendors;

/*
 * When I post to /vendors
 * 201 Status Code
 * Location header with the url for that new vendor.
 * The returned vendor should "look right"
 * AND I should be able to do a GET on that location header and get the same vendor back.
 */

[Collection("VendorsSystemTests")]
public class AddingANewVendor(VendorsFixture fixture)
{

    [Fact]
    public async Task AddingAVendor()
    {
        // I want to add this vendor through the API
        var vendorToPost = new VendorCreateModel
        {
            Name = "Hypertheory",
            Url = "https://www.hypertheory.com",
            PointOfContact = new VendorPointOfContact { Name = "Stacey", Email = "stacey@gmail.com", Phone = "555-1212" }
        };

        // Send it to the API's /vendors resource with POST, make sure you a 201
       var postResponse =  await fixture.Host.Scenario(api =>
        {
            api.WithClaim(new Claim("sub", "jill@company.com"));
            api.WithClaim(new Claim(ClaimTypes.Role, "SoftwareCenter"));
            api.WithClaim(new Claim(ClaimTypes.Role, "Manager"));
            api.Post.Json(vendorToPost).ToUrl("/vendors");
            api.StatusCodeShouldBe(201); // Created
        });

        // Read the body out of the response
        var postBody = postResponse.ReadAsJson<VendorDetailsModel>();

        // verify it copied the stuff over.
        Assert.Equal("Hypertheory", postBody.Name);
        Assert.Equal("https://www.hypertheory.com", postBody.Url);
        Assert.Equal(vendorToPost.PointOfContact, postBody.PointOfContact);

        // Grab the location header
        var location = postResponse.Context.Response.Headers.Location.ToString();

        // Go get that thing from the API (e.g. the database)
        var getResponse = await fixture.Host.Scenario(api =>
        {
            api.Get.Url(location);
            api.StatusCodeShouldBeOk();
        });

        var getBody = getResponse.ReadAsJson<VendorDetailsModel>();

        Assert.Equal(postBody, getBody);

        using var sp = fixture.Host.Services.CreateScope();
        using var db = sp.ServiceProvider.GetRequiredService<IDocumentSession>();
        // look in the database and get the saved entity to check it.
        var savedEntity = await db.LoadAsync<VendorEntity>(postBody.Id);
        Assert.NotNull(savedEntity);
        Assert.Equal("carl@netscape.com", savedEntity.CreatedBy);

  
    }
}
