using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Xunit;

namespace BarberSalon.IntegrationTests;

public class HealthIntegrationTests : IClassFixture<BarberSalonWebFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public HealthIntegrationTests(BarberSalonWebFactory factory)
    {
        _client = factory.GetClient();
    }

    [Fact]
    public async Task GetHealth_Returns200AndHealthyStatus()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<HealthApiResponseEnvelope>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
        envelope.Data!.Status.Should().Be("Healthy");
        envelope.Data.Database.Should().NotBeNull();
        envelope.Data.Database!.Status.Should().Be("Healthy");
        envelope.Data.Database.CanConnect.Should().BeTrue();
        envelope.Data.Environment.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetHealth_WithTrailingSlash_Returns200()
    {
        // Act
        var response = await _client.GetAsync("/health/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<HealthApiResponseEnvelope>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
        envelope.Data!.Status.Should().Be("Healthy");
    }

    [Fact]
    public async Task GetHealth_ApiV1Route_Returns200()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<HealthApiResponseEnvelope>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
        envelope.Data!.Status.Should().Be("Healthy");
    }

    [Fact]
    public async Task GetHealth_WithoutAuthHeader_AllowsAnonymous()
    {
        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Authorization = null;
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    public sealed class HealthApiResponseEnvelope
    {
        public HealthDataDto? Data { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public sealed class HealthDataDto
    {
        public string Status { get; set; } = string.Empty;
        public DatabaseHealthDataDto? Database { get; set; }
        public string Environment { get; set; } = string.Empty;
        public DateTime TimestampUtc { get; set; }
    }

    public sealed class DatabaseHealthDataDto
    {
        public string Status { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public bool CanConnect { get; set; }
    }
}
