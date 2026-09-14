using System.ComponentModel.DataAnnotations;
using FieldCheck.Api.Domain;
using FieldCheck.Api.Validation;

namespace FieldCheck.Api.Contracts;

public record CreateInspectionRequest(
    [Required, NotInFuture] DateTime? InspectedAtUtc,
    [Required, MaxLength(100)] string Inspector,
    [Required] Severity? Severity,
    [MaxLength(2000)] string? Notes);

public record InspectionResponse(
    int Id, int AssetId, DateTime InspectedAtUtc, string Inspector, Severity Severity, string? Notes,
    AssetStatus AssetStatusAfter)
{
    public static InspectionResponse From(Inspection i, AssetStatus assetStatusAfter) => new(
        i.Id, i.AssetId, i.InspectedAtUtc, i.Inspector, i.Severity, i.Notes, assetStatusAfter);
}
