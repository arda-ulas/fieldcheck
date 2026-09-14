using FieldCheck.Api.Contracts;
using FieldCheck.Api.Data;
using FieldCheck.Api.Domain;
using FieldCheck.Api.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FieldCheck.Api.Controllers;

[ApiController]
public class PhotosController(FieldCheckDbContext db, IPhotoStorage storage) : ControllerBase
{
    public const long MaxBytes = 5 * 1024 * 1024;
    public static readonly TimeSpan LinkLifetime = TimeSpan.FromMinutes(10);

    private static readonly Dictionary<string, (string Extension, byte[] Magic)> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = (".jpg", [0xFF, 0xD8, 0xFF]),
        ["image/png"] = (".png", [0x89, 0x50, 0x4E, 0x47]),
    };

    /// <summary>S6: attach a JPEG/PNG (≤ 5 MB) to an inspection. Bytes go to blob storage, metadata to SQL.</summary>
    [HttpPost("api/inspections/{inspectionId:int}/photos")]
    [RequestSizeLimit(MaxBytes + 64 * 1024)] // payload plus multipart overhead
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PhotoResponse>> Upload(int inspectionId, IFormFile? file, CancellationToken ct)
    {
        if (!await db.Inspections.AnyAsync(i => i.Id == inspectionId, ct)) return NotFound();

        if (file is null || file.Length == 0)
        {
            ModelState.AddModelError("file", "A file is required.");
            return ValidationProblem(ModelState);
        }
        if (!Allowed.TryGetValue(file.ContentType, out var kind))
        {
            ModelState.AddModelError("file", "Only image/jpeg and image/png are accepted.");
            return ValidationProblem(ModelState);
        }
        if (file.Length > MaxBytes)
        {
            ModelState.AddModelError("file", $"File exceeds the {MaxBytes / (1024 * 1024)} MB limit.");
            return ValidationProblem(ModelState);
        }

        await using var stream = file.OpenReadStream();
        var header = new byte[kind.Magic.Length];
        var read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, ct);
        if (read < header.Length || !header.AsSpan().SequenceEqual(kind.Magic))
        {
            ModelState.AddModelError("file", "File content does not match its declared content type.");
            return ValidationProblem(ModelState);
        }
        stream.Position = 0;

        var photo = new InspectionPhoto
        {
            InspectionId = inspectionId,
            BlobName = $"inspections/{inspectionId}/{Guid.NewGuid():N}{kind.Extension}",
            ContentType = file.ContentType.ToLowerInvariant(),
            SizeBytes = file.Length,
            UploadedAtUtc = DateTime.UtcNow,
        };

        // Upload first, then record. If the upload fails nothing is recorded; if the insert fails
        // we leave an orphan blob, which is cheap and harmless. The reverse order could record a
        // photo whose bytes do not exist.
        await storage.UploadAsync(photo.BlobName, stream, photo.ContentType, ct);
        db.InspectionPhotos.Add(photo);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { id = photo.Id }, PhotoResponse.From(photo));
    }

    /// <summary>S6: a short-lived read SAS URL for the photo. The container is never public.</summary>
    [HttpGet("api/photos/{id:int}")]
    public async Task<ActionResult<PhotoLinkResponse>> Get(int id, CancellationToken ct)
    {
        var photo = await db.InspectionPhotos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        if (photo is null) return NotFound();

        var expires = DateTime.UtcNow.Add(LinkLifetime);
        return new PhotoLinkResponse(photo.Id, storage.GetReadUri(photo.BlobName, LinkLifetime).ToString(), expires);
    }
}
