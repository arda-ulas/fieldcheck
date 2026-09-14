namespace FieldCheck.Api.Storage;

/// <summary>Where photo bytes live. Metadata stays in SQL; bytes never do.</summary>
public interface IPhotoStorage
{
    Task UploadAsync(string blobName, Stream content, string contentType, CancellationToken ct);

    /// <summary>A read-only URL that expires after <paramref name="lifetime"/>. The container itself is never public.</summary>
    Uri GetReadUri(string blobName, TimeSpan lifetime);
}
