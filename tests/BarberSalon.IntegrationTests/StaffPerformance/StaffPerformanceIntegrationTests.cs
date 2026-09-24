using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BarberSalon.Application.StaffPerformance.DTOs;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Xunit;

namespace BarberSalon.IntegrationTests.StaffPerformance;

public class StaffPerformanceIntegrationTests : IClassFixture<BarberSalonWebFactory>
{
    private readonly HttpClient _client;
    private readonly BarberSalonWebFactory _factory;

    public StaffPerformanceIntegrationTests(BarberSalonWebFactory factory)
    {
        _factory = factory;
        _client = factory.GetClient();
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task GetStaffPerformance_Returns200WithCalculatedMetrics()
    {
        var response = await _client.GetAsync("/api/v1/staff-performance");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<List<StaffPerformanceDto>>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetStaffPerformance_WithDateFilter_Returns200()
    {
        var response = await _client.GetAsync("/api/v1/staff-performance?from=2026-09-01&to=2026-09-30");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<List<StaffPerformanceDto>>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task GetStaffPerformance_WithInvalidFromDate_Returns400()
    {
        var response = await _client.GetAsync("/api/v1/staff-performance?from=not-a-date&to=2026-09-30");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Invalid 'from' date format");
    }

    [Fact]
    public async Task GetStaffPerformance_WithInvertedDateRange_Returns400()
    {
        var response = await _client.GetAsync("/api/v1/staff-performance?from=2026-10-01&to=2026-09-01");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("'from' date cannot be after 'to' date");
    }

    private sealed record ApiResponseEnvelope<T>(T Data, bool Success, string Message);
}
