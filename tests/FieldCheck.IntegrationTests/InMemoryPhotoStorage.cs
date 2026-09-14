using System.Collections.Concurrent;
using FieldCheck.Api.Storage;

namespace FieldCheck.IntegrationTests;

/// <summary>Stands in for Blob Storage so tests need no Azurite; records what was uploaded.</summary>
public sealed class InMemoryPhotoStorage : IPhotoStorage
{
    public ConcurrentDictionary<string, (byte[] Bytes, string ContentType)> Blobs { get; } = new();

    public async Task UploadAsync(string blobName, Stream content, string contentType, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        await content.CopyToAsync(ms, ct);
        Blobs[blobName] = (ms.ToArray(), contentType);
    }

    public Uri GetReadUri(string blobName, TimeSpan lifetime) =>
        new($"https://fake.blob.local/inspection-photos/{blobName}?sig=test&se={DateTime.UtcNow.Add(lifetime):O}");
}
