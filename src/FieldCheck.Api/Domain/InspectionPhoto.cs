namespace FieldCheck.Api.Domain;

public class InspectionPhoto
{
    public int Id { get; set; }
    public int InspectionId { get; set; }
    public Inspection? Inspection { get; set; }
    public required string BlobName { get; set; }
    public required string ContentType { get; set; }
    public long SizeBytes { get; set; }
    public DateTime UploadedAtUtc { get; set; }
}
