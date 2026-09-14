using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FieldCheck.Api.Controllers;

namespace FieldCheck.IntegrationTests;

public class PhotosTests(FieldCheckApiFactory factory) : ApiTestBase(factory)
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4];
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0, 0x10, 0x4A, 0x46, 0x49, 0x46, 1, 2];

    private record PhotoDto(int Id, int InspectionId, string ContentType, long SizeBytes, DateTime UploadedAtUtc);
    private record LinkDto(int Id, string Url, DateTime ExpiresAtUtc);

    private async Task<int> CreateInspectionAsync()
    {
        var site = await CreateSiteAsync();
        var asset = await CreateAssetAsync(site.Id, "PH-1");
        var res = await LogInspectionAsync(asset.Id, "Minor");
        return (await res.Content.ReadFromJsonAsync<InspectionDto>(Json))!.Id;
    }

    private static MultipartFormDataContent Form(byte[] bytes, string contentType, string name = "photo.bin")
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return new MultipartFormDataContent { { file, "file", name } };
    }

    [Fact]
    public async Task Post_Jpeg_StoresBlobAndMetadata()
    {
        var inspectionId = await CreateInspectionAsync();

        var res = await Client.PostAsync($"/api/inspections/{inspectionId}/photos", Form(Jpeg, "image/jpeg", "a.jpg"));

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var photo = await res.Content.ReadFromJsonAsync<PhotoDto>(Json);
        Assert.Equal(inspectionId, photo!.InspectionId);
        Assert.Equal("image/jpeg", photo.ContentType);
        Assert.Equal(Jpeg.Length, photo.SizeBytes);
        var stored = Assert.Single(Factory.Photos.Blobs, b => b.Key.StartsWith($"inspections/{inspectionId}/"));
        Assert.Equal(Jpeg, stored.Value.Bytes);
    }

    [Fact]
    public async Task Post_Png_IsAccepted()
    {
        var inspectionId = await CreateInspectionAsync();
        var res = await Client.PostAsync($"/api/inspections/{inspectionId}/photos", Form(Png, "image/png", "a.png"));
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task Post_UnsupportedContentType_Returns400()
    {
        var inspectionId = await CreateInspectionAsync();
        var res = await Client.PostAsync($"/api/inspections/{inspectionId}/photos", Form([1, 2, 3], "application/pdf", "a.pdf"));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var problem = await res.Content.ReadFromJsonAsync<ProblemDto>(Json);
        Assert.Contains("file", problem!.Errors!.Keys);
    }

    [Fact]
    public async Task Post_ContentNotMatchingType_Returns400()
    {
        var inspectionId = await CreateInspectionAsync();
        var res = await Client.PostAsync($"/api/inspections/{inspectionId}/photos", Form([0x25, 0x50, 0x44, 0x46], "image/png", "fake.png"));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Post_Oversize_Returns400()
    {
        var inspectionId = await CreateInspectionAsync();
        var big = new byte[PhotosController.MaxBytes + 1];
        Png.CopyTo(big, 0);
        var res = await Client.PostAsync($"/api/inspections/{inspectionId}/photos", Form(big, "image/png", "big.png"));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Post_UnknownInspection_Returns404()
    {
        var res = await Client.PostAsync("/api/inspections/999999/photos", Form(Png, "image/png"));
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Get_ReturnsShortLivedSasUrl()
    {
        var inspectionId = await CreateInspectionAsync();
        var created = await (await Client.PostAsync($"/api/inspections/{inspectionId}/photos", Form(Png, "image/png")))
            .Content.ReadFromJsonAsync<PhotoDto>(Json);

        var link = await Client.GetFromJsonAsync<LinkDto>($"/api/photos/{created!.Id}", Json);

        Assert.Contains("sig=", link!.Url);
        Assert.InRange(link.ExpiresAtUtc, DateTime.UtcNow.AddMinutes(5), DateTime.UtcNow.AddMinutes(15));
    }

    [Fact]
    public async Task Get_UnknownPhoto_Returns404()
    {
        var res = await Client.GetAsync("/api/photos/999999");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
}
