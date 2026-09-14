using System.Net;
using System.Net.Http.Json;

namespace FieldCheck.IntegrationTests;

public class AssetStatusTests(FieldCheckApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Put_WithCurrentRowVersion_Returns200()
    {
        var site = await CreateSiteAsync();
        var asset = await CreateAssetAsync(site.Id, "MTR-1");
        await LogInspectionAsync(asset.Id, "Critical");
        var current = await GetAssetAsync(asset.Id);
        Assert.Equal("OutOfService", current.Status);

        var res = await Client.PutAsJsonAsync($"/api/assets/{asset.Id}/status",
            new { status = "InService", rowVersion = current.RowVersion }, Json);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var updated = await res.Content.ReadFromJsonAsync<AssetDto>(Json);
        Assert.Equal("InService", updated!.Status);
        Assert.NotEqual(current.RowVersion, updated.RowVersion);
    }

    [Fact]
    public async Task Put_WithStaleRowVersion_Returns409()
    {
        var site = await CreateSiteAsync();
        var asset = await CreateAssetAsync(site.Id, "MTR-2");
        var stale = asset.RowVersion;

        // Someone else changes the row first (the Critical inspection bumps the rowversion).
        await LogInspectionAsync(asset.Id, "Critical");

        var res = await Client.PutAsJsonAsync($"/api/assets/{asset.Id}/status",
            new { status = "InService", rowVersion = stale }, Json);

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal("OutOfService", (await GetAssetAsync(asset.Id)).Status);
    }

    [Fact]
    public async Task Put_WithInvalidBase64_Returns400()
    {
        var site = await CreateSiteAsync();
        var asset = await CreateAssetAsync(site.Id, "MTR-3");
        var res = await Client.PutAsJsonAsync($"/api/assets/{asset.Id}/status",
            new { status = "InService", rowVersion = "not base64!" }, Json);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }
}
