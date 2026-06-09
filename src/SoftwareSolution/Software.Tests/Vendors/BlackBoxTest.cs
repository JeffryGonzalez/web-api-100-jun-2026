
using System.Net;

namespace Software.Tests.Vendors;

public class BlackBoxTest
{
    [Fact(Skip = "Demo - Black Box")]
    public async Task GettingAVendorThatDoesntExistNotFound()
    {
        using var client = new HttpClient();
        client.BaseAddress = new Uri("https://localhost:9000");

        var response = await client.GetAsync("/vendors/" + Guid.Empty);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
