using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace FieldCheck.Api.Storage;

public sealed class BlobPhotoStorage(BlobContainerClient container) : IPhotoStorage
{
    public async Task UploadAsync(string blobName, Stream content, string contentType, CancellationToken ct)
    {
        // PublicAccessType.None: blobs are only reachable through the API's SAS URLs.
        await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);
        await container.GetBlobClient(blobName).UploadAsync(content,
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } }, ct);
    }

    public Uri GetReadUri(string blobName, TimeSpan lifetime)
    {
        var blob = container.GetBlobClient(blobName);
        if (!blob.CanGenerateSasUri)
        {
            throw new InvalidOperationException("Blob client cannot sign SAS URLs; configure a shared-key connection string.");
        }
        return blob.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.Add(lifetime));
    }
}
