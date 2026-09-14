using System.Net;

namespace FieldCheck.IntegrationTests;

public class HealthTests(FieldCheckApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Health_ReportsHealthy_WhenDatabaseReachable()
    {
        var res = await Client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal("Healthy", await res.Content.ReadAsStringAsync());
    }
}
