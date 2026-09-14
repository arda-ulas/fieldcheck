using System.Net;
using System.Net.Http.Json;

namespace FieldCheck.IntegrationTests;

public class AssetsTests(FieldCheckApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Post_ValidAsset_Returns201WithLocation()
    {
        var site = await CreateSiteAsync();

        var res = await Client.PostAsJsonAsync($"/api/sites/{site.Id}/assets",
            new { tag = "PMP-104", name = "Feed pump", category = "Pump", inspectionIntervalDays = 30 }, Json);

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<AssetDto>(Json);
        Assert.NotNull(res.Headers.Location);
        Assert.EndsWith($"/api/assets/{body!.Id}", res.Headers.Location!.ToString());
        Assert.Equal("InService", body.Status);
        Assert.NotEmpty(body.RowVersion);
    }

    [Fact]
    public async Task Post_DuplicateTagAtSameSite_Returns409()
    {
        var site = await CreateSiteAsync();
        await CreateAssetAsync(site.Id, "CNV-201");

        var res = await Client.PostAsJsonAsync($"/api/sites/{site.Id}/assets",
            new { tag = "CNV-201", name = "Again", category = "Conveyor", inspectionIntervalDays = 14 }, Json);

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        var problem = await res.Content.ReadFromJsonAsync<ProblemDto>(Json);
        Assert.Contains("already exists", problem!.Title);
    }

    [Fact]
    public async Task Post_SameTagAtDifferentSite_IsAllowed()
    {
        var a = await CreateSiteAsync();
        var b = await CreateSiteAsync();
        await CreateAssetAsync(a.Id, "TRF-301");
        var second = await CreateAssetAsync(b.Id, "TRF-301");
        Assert.Equal(b.Id, second.SiteId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(366)]
    public async Task Post_IntervalOutOfRange_Returns400NamingField(int interval)
    {
        var site = await CreateSiteAsync();

        var res = await Client.PostAsJsonAsync($"/api/sites/{site.Id}/assets",
            new { tag = "X-1", name = "x", category = "x", inspectionIntervalDays = interval }, Json);

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Equal("application/problem+json", res.Content.Headers.ContentType!.MediaType);
        var problem = await res.Content.ReadFromJsonAsync<ProblemDto>(Json);
        Assert.Contains("InspectionIntervalDays", problem!.Errors!.Keys);
    }

    [Fact]
    public async Task Post_ToUnknownSite_Returns404()
    {
        var res = await Client.PostAsJsonAsync("/api/sites/999999/assets",
            new { tag = "X-1", name = "x", category = "x", inspectionIntervalDays = 10 }, Json);
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
}
