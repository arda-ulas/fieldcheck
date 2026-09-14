using FieldCheck.Api.Contracts;
using FieldCheck.Api.Data;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.EntityFrameworkCore;

namespace FieldCheck.Api.OData;

/// <summary>
/// S5: GET /odata/Inspections with $filter, $orderby, $top (max 100), $skip, $count, $select.
/// The query is applied to an EF projection, so filtering and paging happen in SQL Server.
/// </summary>
public class InspectionsController(FieldCheckDbContext db) : ODataController
{
    [EnableQuery(MaxTop = 100, PageSize = 100)]
    public IQueryable<InspectionRecord> Get() =>
        db.Inspections.AsNoTracking().Select(i => new InspectionRecord
        {
            Id = i.Id,
            AssetId = i.AssetId,
            AssetTag = i.Asset!.Tag,
            SiteId = i.Asset.SiteId,
            InspectedAtUtc = i.InspectedAtUtc,
            Inspector = i.Inspector,
            Severity = i.Severity,
            Notes = i.Notes,
        });
}
