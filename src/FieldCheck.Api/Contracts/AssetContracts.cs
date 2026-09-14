using System.ComponentModel.DataAnnotations;
using FieldCheck.Api.Domain;

namespace FieldCheck.Api.Contracts;

public record CreateAssetRequest(
    [Required, MaxLength(50)] string Tag,
    [Required, MaxLength(200)] string Name,
    [Required, MaxLength(50)] string Category,
    [Range(1, 365)] int InspectionIntervalDays);

/// <summary>RowVersion is the base64 form of the SQL Server rowversion the client last read.</summary>
public record UpdateAssetStatusRequest(
    [Required] AssetStatus? Status,
    [Required] string RowVersion);

public record AssetResponse(
    int Id, int SiteId, string Tag, string Name, string Category,
    int InspectionIntervalDays, AssetStatus Status, string RowVersion)
{
    public static AssetResponse From(Asset a) => new(
        a.Id, a.SiteId, a.Tag, a.Name, a.Category, a.InspectionIntervalDays, a.Status,
        Convert.ToBase64String(a.RowVersion));
}
