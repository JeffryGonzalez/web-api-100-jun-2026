using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace Software.Tests.Vendors;

[Collection("VendorsSystemTests")]
public class NonManagersTryToAddVendor(VendorsFixture fixture)
{
    [Fact]
    public async Task NoAuthGets401()
    {
        await fixture.Host.Scenario(api =>
        {
            api.Post.Json(new { }).ToUrl("/vendors");
            api.StatusCodeShouldBe(403); // Unauthorized
        });
    }


    [Fact]
    public async Task NonManager()
    {
        await fixture.Host.Scenario(api =>
        {
            api.WithClaim(new System.Security.Claims.Claim("sub", "joe@aol.com"));
            api.WithClaim(new System.Security.Claims.Claim(ClaimTypes.Role, "SoftwareCenter"));
            api.Post.Json(new { }).ToUrl("/vendors");
            api.StatusCodeShouldBe(403); // Unauthorized
        });
    }
}



