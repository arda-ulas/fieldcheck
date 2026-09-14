using FieldCheck.Api.Contracts;
using FieldCheck.Api.Data;
using FieldCheck.Api.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FieldCheck.Api.Controllers;

[ApiController]
[Route("api/sites")]
[Produces("application/json")]
public class SitesController(FieldCheckDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SiteResponse>>> List(CancellationToken ct) =>
        await db.Sites.AsNoTracking().OrderBy(s => s.Name).Select(s => SiteResponse.From(s)).ToListAsync(ct);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SiteResponse>> Get(int id, CancellationToken ct)
    {
        var site = await db.Sites.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);
        return site is null ? NotFound() : SiteResponse.From(site);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SiteResponse>> Create(CreateSiteRequest request, CancellationToken ct)
    {
        if (await db.Sites.AnyAsync(s => s.Name == request.Name, ct))
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "Site name already exists.");
        }

        var site = new Site { Name = request.Name, Region = request.Region };
        db.Sites.Add(site);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = site.Id }, SiteResponse.From(site));
    }
}
