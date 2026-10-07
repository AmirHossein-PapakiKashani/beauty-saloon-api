using System.Net;
using System.Net.Http.Json;
using BarberSalon.Application.PlatformAdmin.DTOs;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Xunit;

namespace BarberSalon.IntegrationTests.PlatformAdmin;

public class PlatformAdminControllerTests : IClassFixture<BarberSalonWebFactory>
{
    private readonly HttpClient _client;

    public PlatformAdminControllerTests(BarberSalonWebFactory factory)
    {
        _client = factory.GetClient();
    }

    [Fact]
    public async Task GetMetrics_ShouldReturnOkWithMetricsPayload()
    {
        var response = await _client.GetAsync("/api/v1/platform-admin/metrics");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetSalons_ShouldReturnOkList()
    {
        var response = await _client.GetAsync("/api/v1/platform-admin/salons");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
