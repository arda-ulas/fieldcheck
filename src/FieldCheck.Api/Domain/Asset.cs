namespace FieldCheck.Api.Domain;

public class Asset
{
    public int Id { get; set; }
    public int SiteId { get; set; }
    public Site? Site { get; set; }
    public required string Tag { get; set; }
    public required string Name { get; set; }
    public required string Category { get; set; }
    public int InspectionIntervalDays { get; set; }
    public AssetStatus Status { get; set; } = AssetStatus.InService;
    public byte[] RowVersion { get; set; } = [];
    public ICollection<Inspection> Inspections { get; set; } = [];
}
