namespace FieldCheck.Api.Domain;

public class Site
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Region { get; set; }
    public ICollection<Asset> Assets { get; set; } = [];
}
