using FieldCheck.Api.Domain;

namespace FieldCheck.Api.Contracts;

/// <summary>One row of dbo.usp_GetOverdueAssets. Property names match the procedure's column names.</summary>
public class OverdueAssetRow
{
    public int AssetId { get; set; }
    public int SiteId { get; set; }
    public string SiteName { get; set; } = "";
    public string Tag { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public int InspectionIntervalDays { get; set; }
    public DateTime? LastInspectedAtUtc { get; set; }
    public string? LastSeverity { get; set; }
    public bool NeverInspected { get; set; }
    public int? DaysOverdue { get; set; }
}

/// <summary>Flat, read-only projection exposed through OData. Not an EF entity.</summary>
public class InspectionRecord
{
    public int Id { get; set; }
    public int AssetId { get; set; }
    public string AssetTag { get; set; } = "";
    public int SiteId { get; set; }
    public DateTime InspectedAtUtc { get; set; }
    public string Inspector { get; set; } = "";
    public Severity Severity { get; set; }
    public string? Notes { get; set; }
}
