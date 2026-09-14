using FieldCheck.Api.Contracts;
using FieldCheck.Api.Data;
using FieldCheck.Api.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FieldCheck.Api.Controllers;

[ApiController]
[Produces("application/json")]
public class InspectionsController(FieldCheckDbContext db) : ControllerBase
{
    [HttpGet("api/inspections/{id:int}")]
    public async Task<ActionResult<InspectionResponse>> Get(int id, CancellationToken ct)
    {
        var inspection = await db.Inspections.AsNoTracking().Include(i => i.Asset)
            .FirstOrDefaultAsync(i => i.Id == id, ct);
        return inspection is null ? NotFound() : InspectionResponse.From(inspection, inspection.Asset!.Status);
    }

    /// <summary>
    /// S2: log an inspection. A Critical finding also takes the asset out of service. Both rows
    /// are written by one SaveChanges call, which EF Core wraps in a single database
    /// transaction, so either both changes persist or neither does.
    /// </summary>
    [HttpPost("api/assets/{assetId:int}/inspections")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InspectionResponse>> Create(int assetId, CreateInspectionRequest request, CancellationToken ct)
    {
        var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == assetId, ct);
        if (asset is null) return NotFound();

        var inspection = new Inspection
        {
            AssetId = assetId,
            InspectedAtUtc = DateTime.SpecifyKind(request.InspectedAtUtc!.Value, DateTimeKind.Utc),
            Inspector = request.Inspector,
            Severity = request.Severity!.Value,
            Notes = request.Notes,
        };
        db.Inspections.Add(inspection);
        asset.Status = InspectionRules.StatusAfter(asset.Status, inspection.Severity);

        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { id = inspection.Id }, InspectionResponse.From(inspection, asset.Status));
    }
}
