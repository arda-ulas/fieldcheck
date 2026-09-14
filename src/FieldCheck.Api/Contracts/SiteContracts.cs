using System.ComponentModel.DataAnnotations;
using FieldCheck.Api.Domain;

namespace FieldCheck.Api.Contracts;

public record CreateSiteRequest(
    [Required, MaxLength(100)] string Name,
    [Required, MaxLength(100)] string Region);

public record SiteResponse(int Id, string Name, string Region)
{
    public static SiteResponse From(Site s) => new(s.Id, s.Name, s.Region);
}
