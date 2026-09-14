using FieldCheck.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace FieldCheck.Api.Data.Seed;

/// <summary>
/// Seeds one demo site with ~10 assets and a spread of inspections so the overdue report and
/// OData queries have something to show. Idempotent: does nothing if the demo site exists.
/// Runs only when explicitly invoked (Development startup or a deliberate command); it never
/// applies migrations.
/// </summary>
public static class DemoSeeder
{
    public const string DemoSiteName = "Northgate Processing Plant";

    public static async Task SeedAsync(FieldCheckDbContext db, CancellationToken ct = default)
    {
        if (await db.Sites.AnyAsync(s => s.Name == DemoSiteName, ct))
        {
            return;
        }

        var now = DateTime.UtcNow;
        var site = new Site { Name = DemoSiteName, Region = "Ontario" };

        Asset A(string tag, string name, string category, int interval, AssetStatus status = AssetStatus.InService)
            => new() { Tag = tag, Name = name, Category = category, InspectionIntervalDays = interval, Status = status, Site = site };

        var pmp104 = A("PMP-104", "Slurry feed pump", "Pump", 30);
        var pmp105 = A("PMP-105", "Cooling water pump", "Pump", 30);
        var cnv201 = A("CNV-201", "Ore conveyor, section 1", "Conveyor", 14);
        var cnv202 = A("CNV-202", "Ore conveyor, section 2", "Conveyor", 14);
        var trf301 = A("TRF-301", "Main substation transformer", "Transformer", 90);
        var trf302 = A("TRF-302", "Mill transformer", "Transformer", 90);
        var cmp401 = A("CMP-401", "Instrument air compressor", "Compressor", 60);
        var mtr501 = A("MTR-501", "Mill drive motor", "Motor", 45);
        var vlv601 = A("VLV-601", "Tailings isolation valve", "Valve", 180);
        var crn701 = A("CRN-701", "Maintenance bay crane", "Crane", 365, AssetStatus.OutOfService);

        Inspection I(Asset asset, int daysAgo, Severity severity, string inspector, string? notes = null)
            => new() { Asset = asset, InspectedAtUtc = now.AddDays(-daysAgo), Severity = severity, Inspector = inspector, Notes = notes };

        var inspections = new[]
        {
            I(pmp104, 5, Severity.None, "J. Okafor"),
            I(pmp104, 40, Severity.Minor, "J. Okafor", "Slight seal weep, monitored."),
            I(pmp105, 45, Severity.Minor, "M. Tremblay", "Bearing noise, lubricated."),   // overdue by 15
            I(cnv201, 3, Severity.None, "S. Patel"),
            I(cnv202, 20, Severity.Major, "S. Patel", "Belt tracking off-centre, adjusted."), // overdue by 6
            I(trf301, 100, Severity.None, "L. Nguyen"),                                       // overdue by 10
            I(trf302, 10, Severity.None, "L. Nguyen"),
            I(cmp401, 30, Severity.None, "M. Tremblay"),
            I(crn701, 2, Severity.Critical, "R. Singh", "Hoist brake failed load test. Taken out of service."),
            // MTR-501 and VLV-601 are never inspected: they must appear at the top of the overdue report.
        };

        db.Sites.Add(site);
        db.Inspections.AddRange(inspections);
        db.Assets.AddRange(mtr501, vlv601);
        await db.SaveChangesAsync(ct);
    }
}
