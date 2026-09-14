using FieldCheck.Api.Domain;

namespace FieldCheck.Api.Contracts;

public record PhotoResponse(int Id, int InspectionId, string ContentType, long SizeBytes, DateTime UploadedAtUtc)
{
    public static PhotoResponse From(InspectionPhoto p) => new(p.Id, p.InspectionId, p.ContentType, p.SizeBytes, p.UploadedAtUtc);
}

public record PhotoLinkResponse(int Id, string Url, DateTime ExpiresAtUtc);
