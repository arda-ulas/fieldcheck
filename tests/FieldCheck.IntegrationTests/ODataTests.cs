using System.Net;
using System.Text.Json;

namespace FieldCheck.IntegrationTests;

public class ODataTests(FieldCheckApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task FilterOrderTopCount_Works()
    {
        var site = await CreateSiteAsync();
        var asset = await CreateAssetAsync(site.Id, "OD-1");
        await LogInspectionAsync(asset.Id, "Minor", DateTime.UtcNow.AddDays(-3));
        await LogInspectionAsync(asset.Id, "Critical", DateTime.UtcNow.AddDays(-2));

        var res = await Client.GetAsync(
            $"/odata/Inspections?$filter=Severity eq 'Critical' and SiteId eq {site.Id}&$orderby=InspectedAtUtc desc&$top=10&$count=true");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        Assert.Equal(1, doc.RootElement.GetProperty("@odata.count").GetInt32());
        var item = Assert.Single(doc.RootElement.GetProperty("value").EnumerateArray());
        Assert.Equal("Critical", item.GetProperty("Severity").GetString());
        Assert.Equal("OD-1", item.GetProperty("AssetTag").GetString());
        Assert.EndsWith("Z", item.GetProperty("InspectedAtUtc").GetString());
    }

    [Fact]
    public async Task OrderByDesc_ReturnsNewestFirst()
    {
        var site = await CreateSiteAsync();
        var asset = await CreateAssetAsync(site.Id, "OD-2");
        await LogInspectionAsync(asset.Id, "None", DateTime.UtcNow.AddDays(-5));
        await LogInspectionAsync(asset.Id, "Major", DateTime.UtcNow.AddDays(-1));

        var res = await Client.GetAsync($"/odata/Inspections?$filter=AssetId eq {asset.Id}&$orderby=InspectedAtUtc desc");
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        var severities = doc.RootElement.GetProperty("value").EnumerateArray().Select(e => e.GetProperty("Severity").GetString()!).ToArray();
        Assert.Equal(["Major", "None"], severities);
    }

    [Fact]
    public async Task TopAboveCap_Returns400()
    {
        var res = await Client.GetAsync("/odata/Inspections?$top=101");
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Theory]
    [InlineData("$expand=Asset")]
    [InlineData("$search=pump")]
    [InlineData("$apply=groupby((Severity))")]
    public async Task UnsupportedOption_Returns400(string query)
    {
        var res = await Client.GetAsync($"/odata/Inspections?{query}");
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }
}
