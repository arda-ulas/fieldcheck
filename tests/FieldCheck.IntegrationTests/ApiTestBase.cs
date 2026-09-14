using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FieldCheck.IntegrationTests;

[Collection(ApiCollection.Name)]
public abstract class ApiTestBase(FieldCheckApiFactory factory)
{
    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    protected HttpClient Client { get; } = factory.CreateClient();

    protected record SiteDto(int Id, string Name, string Region);
    protected record AssetDto(int Id, int SiteId, string Tag, string Name, string Category, int InspectionIntervalDays, string Status, string RowVersion);
    protected record InspectionDto(int Id, int AssetId, DateTime InspectedAtUtc, string Inspector, string Severity, string? Notes, string AssetStatusAfter);
    protected record OverdueRow(int AssetId, string Tag, DateTime? LastInspectedAtUtc, string? LastSeverity, bool NeverInspected, int? DaysOverdue);
    protected record ProblemDto(string? Title, int? Status, Dictionary<string, string[]>? Errors);

    /// <summary>Each test gets its own site so tests never see each other's assets.</summary>
    protected async Task<SiteDto> CreateSiteAsync()
    {
        var res = await Client.PostAsJsonAsync("/api/sites", new { name = $"Test Site {Guid.NewGuid():N}", region = "Test" }, Json);
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<SiteDto>(Json))!;
    }

    protected async Task<AssetDto> CreateAssetAsync(int siteId, string tag, int intervalDays = 30)
    {
        var res = await Client.PostAsJsonAsync($"/api/sites/{siteId}/assets",
            new { tag, name = $"Asset {tag}", category = "Pump", inspectionIntervalDays = intervalDays }, Json);
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<AssetDto>(Json))!;
    }

    protected async Task<HttpResponseMessage> LogInspectionAsync(int assetId, string severity, DateTime? at = null, string? notes = null) =>
        await Client.PostAsJsonAsync($"/api/assets/{assetId}/inspections",
            new { inspectedAtUtc = at ?? DateTime.UtcNow.AddMinutes(-1), inspector = "Test Inspector", severity, notes }, Json);

    protected async Task<AssetDto> GetAssetAsync(int id) =>
        (await Client.GetFromJsonAsync<AssetDto>($"/api/assets/{id}", Json))!;
}
