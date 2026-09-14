using FieldCheck.Api.Contracts;
using FieldCheck.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FieldCheck.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Produces("application/json")]
public class ReportsController(FieldCheckDbContext db) : ControllerBase
{
    /// <summary>
    /// S4: overdue assets from dbo.usp_GetOverdueAssets. The interpolated value is sent as a
    /// SQL parameter by EF Core's SqlQuery (it is a FormattableString, not string concatenation).
    /// </summary>
    [HttpGet("overdue-assets")]
    public async Task<ActionResult<IEnumerable<OverdueAssetRow>>> OverdueAssets([FromQuery] int? siteId, CancellationToken ct) =>
        await db.Database
            .SqlQuery<OverdueAssetRow>($"EXEC dbo.usp_GetOverdueAssets @SiteId = {siteId}")
            .ToListAsync(ct);
}
