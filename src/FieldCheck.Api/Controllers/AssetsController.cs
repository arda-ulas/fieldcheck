using FieldCheck.Api.Contracts;
using FieldCheck.Api.Data;
using FieldCheck.Api.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FieldCheck.Api.Controllers;

[ApiController]
[Produces("application/json")]
public class AssetsController(FieldCheckDbContext db) : ControllerBase
{
    // SQL Server error numbers for unique index / unique constraint violations.
    private const int DuplicateKeyIndex = 2601;
    private const int DuplicateKeyConstraint = 2627;

    [HttpGet("api/sites/{siteId:int}/assets")]
    public async Task<ActionResult<IEnumerable<AssetResponse>>> ListForSite(int siteId, CancellationToken ct)
    {
        if (!await db.Sites.AnyAsync(s => s.Id == siteId, ct)) return NotFound();
        return await db.Assets.AsNoTracking().Where(a => a.SiteId == siteId).OrderBy(a => a.Tag)
            .Select(a => AssetResponse.From(a)).ToListAsync(ct);
    }

    [HttpGet("api/assets/{id:int}")]
    public async Task<ActionResult<AssetResponse>> Get(int id, CancellationToken ct)
    {
        var asset = await db.Assets.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);
        return asset is null ? NotFound() : AssetResponse.From(asset);
    }

    /// <summary>S1: register an asset at a site. Tag must be unique per site.</summary>
    [HttpPost("api/sites/{siteId:int}/assets")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AssetResponse>> Create(int siteId, CreateAssetRequest request, CancellationToken ct)
    {
        if (!await db.Sites.AnyAsync(s => s.Id == siteId, ct)) return NotFound();

        var asset = new Asset
        {
            SiteId = siteId,
            Tag = request.Tag,
            Name = request.Name,
            Category = request.Category,
            InspectionIntervalDays = request.InspectionIntervalDays,
        };
        db.Assets.Add(asset);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: DuplicateKeyIndex or DuplicateKeyConstraint })
        {
            // Let the unique index IX_Assets_SiteId_Tag be the single source of truth for
            // duplicates; a pre-check would still race with concurrent inserts.
            return Problem(statusCode: StatusCodes.Status409Conflict,
                title: "Asset tag already exists at this site.",
                detail: $"Tag '{request.Tag}' is already registered at site {siteId}.");
        }

        return CreatedAtAction(nameof(Get), new { id = asset.Id }, AssetResponse.From(asset));
    }

    /// <summary>S3: change status with optimistic concurrency on RowVersion.</summary>
    [HttpPut("api/assets/{id:int}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AssetResponse>> UpdateStatus(int id, UpdateAssetStatusRequest request, CancellationToken ct)
    {
        byte[] rowVersion;
        try
        {
            rowVersion = Convert.FromBase64String(request.RowVersion);
        }
        catch (FormatException)
        {
            ModelState.AddModelError(nameof(request.RowVersion), "RowVersion must be base64.");
            return ValidationProblem(ModelState);
        }

        var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (asset is null) return NotFound();

        // Tell EF the version the client last saw. The generated UPDATE adds
        // "WHERE Id = @id AND RowVersion = @original"; zero rows affected means someone else
        // changed the row first, and EF raises DbUpdateConcurrencyException.
        db.Entry(asset).Property(a => a.RowVersion).OriginalValue = rowVersion;
        asset.Status = request.Status!.Value;

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict,
                title: "The asset was modified by someone else.",
                detail: "Reload the asset and retry with its current RowVersion.");
        }

        return AssetResponse.From(asset);
    }
}
