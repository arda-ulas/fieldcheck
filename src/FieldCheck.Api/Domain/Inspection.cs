namespace FieldCheck.Api.Domain;

public class Inspection
{
    public int Id { get; set; }
    public int AssetId { get; set; }
    public Asset? Asset { get; set; }
    public DateTime InspectedAtUtc { get; set; }
    public required string Inspector { get; set; }
    public Severity Severity { get; set; }
    public string? Notes { get; set; }
    public ICollection<InspectionPhoto> Photos { get; set; } = [];
}
