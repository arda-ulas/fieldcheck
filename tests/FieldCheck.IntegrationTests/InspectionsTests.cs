using System.Net;
using System.Net.Http.Json;

namespace FieldCheck.IntegrationTests;

public class InspectionsTests(FieldCheckApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Post_Minor_KeepsAssetInService()
    {
        var site = await CreateSiteAsync();
        var asset = await CreateAssetAsync(site.Id, "PMP-1");

        var res = await LogInspectionAsync(asset.Id, "Minor");

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<InspectionDto>(Json);
        Assert.Equal("InService", body!.AssetStatusAfter);
        Assert.Equal("InService", (await GetAssetAsync(asset.Id)).Status);
    }

    [Fact]
    public async Task Post_Critical_SavesAndTakesAssetOutOfService_Atomically()
    {
        var site = await CreateSiteAsync();
        var asset = await CreateAssetAsync(site.Id, "PMP-2");

        var res = await LogInspectionAsync(asset.Id, "Critical", notes: "Seal failure");

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var created = await res.Content.ReadFromJsonAsync<InspectionDto>(Json);

        // Both effects must be visible on re-read: the inspection row and the status change.
        var stored = await Client.GetFromJsonAsync<InspectionDto>($"/api/inspections/{created!.Id}", Json);
        Assert.Equal("Critical", stored!.Severity);
        Assert.Equal("OutOfService", stored.AssetStatusAfter);
        Assert.Equal("OutOfService", (await GetAssetAsync(asset.Id)).Status);
    }

    [Fact]
    public async Task Post_FutureDate_Returns400()
    {
        var site = await CreateSiteAsync();
        var asset = await CreateAssetAsync(site.Id, "PMP-3");

        var res = await LogInspectionAsync(asset.Id, "None", at: DateTime.UtcNow.AddDays(1));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var problem = await res.Content.ReadFromJsonAsync<ProblemDto>(Json);
        Assert.Contains("InspectedAtUtc", problem!.Errors!.Keys);
    }

    [Fact]
    public async Task Post_ToUnknownAsset_Returns404()
    {
        var res = await LogInspectionAsync(999999, "None");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
}
