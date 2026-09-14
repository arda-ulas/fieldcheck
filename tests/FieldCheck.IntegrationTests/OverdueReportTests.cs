using System.Net.Http.Json;

namespace FieldCheck.IntegrationTests;

/// <summary>Exercises dbo.usp_GetOverdueAssets through the API against real SQL Server.</summary>
public class OverdueReportTests(FieldCheckApiFactory factory) : ApiTestBase(factory)
{
    private async Task<(int siteId, AssetDto never, AssetDto ten, AssetDto twenty, AssetDto fresh, AssetDto oos)> ArrangeAsync()
    {
        var site = await CreateSiteAsync();
        var never = await CreateAssetAsync(site.Id, "NEVER", 30);
        var ten = await CreateAssetAsync(site.Id, "TEN", 30);
        var twenty = await CreateAssetAsync(site.Id, "TWENTY", 30);
        var fresh = await CreateAssetAsync(site.Id, "FRESH", 30);
        var oos = await CreateAssetAsync(site.Id, "OOS", 30);

        (await LogInspectionAsync(ten.Id, "Minor", DateTime.UtcNow.AddDays(-40))).EnsureSuccessStatusCode();
        (await LogInspectionAsync(twenty.Id, "None", DateTime.UtcNow.AddDays(-50))).EnsureSuccessStatusCode();
        (await LogInspectionAsync(fresh.Id, "None", DateTime.UtcNow.AddDays(-1))).EnsureSuccessStatusCode();
        (await LogInspectionAsync(oos.Id, "Critical", DateTime.UtcNow.AddDays(-60))).EnsureSuccessStatusCode();
        return (site.Id, never, ten, twenty, fresh, oos);
    }

    private async Task<List<OverdueRow>> ReportAsync(int siteId) =>
        (await Client.GetFromJsonAsync<List<OverdueRow>>($"/api/reports/overdue-assets?siteId={siteId}", Json))!;

    [Fact]
    public async Task NeverInspectedAsset_IsOverdue()
    {
        var (siteId, never, _, _, _, _) = await ArrangeAsync();
        var rows = await ReportAsync(siteId);
        var row = Assert.Single(rows, r => r.AssetId == never.Id);
        Assert.True(row.NeverInspected);
        Assert.Null(row.DaysOverdue);
        Assert.Null(row.LastInspectedAtUtc);
    }

    [Fact]
    public async Task Report_OrdersMostOverdueFirst_WithDaysOverdue()
    {
        var (siteId, never, ten, twenty, fresh, _) = await ArrangeAsync();
        var rows = await ReportAsync(siteId);

        Assert.Equal(["NEVER", "TWENTY", "TEN"], rows.Select(r => r.Tag).ToArray());
        Assert.Equal(20, rows.Single(r => r.AssetId == twenty.Id).DaysOverdue);
        Assert.Equal(10, rows.Single(r => r.AssetId == ten.Id).DaysOverdue);
        Assert.Equal("Minor", rows.Single(r => r.AssetId == ten.Id).LastSeverity);
        Assert.DoesNotContain(rows, r => r.AssetId == fresh.Id);
    }

    [Fact]
    public async Task OutOfServiceAsset_IsExcluded()
    {
        var (siteId, _, _, _, _, oos) = await ArrangeAsync();
        var rows = await ReportAsync(siteId);
        Assert.DoesNotContain(rows, r => r.AssetId == oos.Id);
    }

    [Fact]
    public async Task Report_WithoutSiteId_SpansSites()
    {
        var (siteA, _, _, _, _, _) = await ArrangeAsync();
        var (siteB, _, _, _, _, _) = await ArrangeAsync();
        var all = await Client.GetFromJsonAsync<List<OverdueRow>>("/api/reports/overdue-assets", Json);
        Assert.True(all!.Count >= 6);
        var a = await ReportAsync(siteA);
        var b = await ReportAsync(siteB);
        Assert.Equal(3, a.Count);
        Assert.Equal(3, b.Count);
    }
}
