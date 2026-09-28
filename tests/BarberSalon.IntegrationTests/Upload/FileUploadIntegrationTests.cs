using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Xunit;

namespace BarberSalon.IntegrationTests.Upload;

public class FileUploadIntegrationTests : IClassFixture<BarberSalonWebFactory>
{
    private readonly HttpClient _client;

    public FileUploadIntegrationTests(BarberSalonWebFactory factory)
        => _client = factory.GetClient();

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task Upload_ValidJpegFile_Returns200WithUrl()
    {
        // Minimal valid JPEG header bytes
        byte[] jpegBytes = { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46 };
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(jpegBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        content.Add(fileContent, "file", "test.jpg");

        var response = await _client.PostAsync("/api/v1/upload", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content
            .ReadFromJsonAsync<UploadEnvelope>(JsonOptions);
        envelope!.Success.Should().BeTrue();
        envelope.Data.Url.Should().NotBeNullOrWhiteSpace();
        envelope.Data.Url.Should().Contain("/uploads/");
    }

    [Fact]
    public async Task Upload_TextFile_Returns400()
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 0x41, 0x42, 0x43 });
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "file", "test.txt");

        var response = await _client.PostAsync("/api/v1/upload", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Upload_NoFile_Returns400()
    {
        using var content = new MultipartFormDataContent();
        var response = await _client.PostAsync("/api/v1/upload", content);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed record UploadResultData(string Url);
    private sealed record UploadEnvelope(UploadResultData Data, bool Success, string Message);
}
