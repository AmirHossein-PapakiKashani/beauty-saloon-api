using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BarberSalon.Application.Admin.DTOs;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Xunit;

namespace BarberSalon.IntegrationTests.Admin;

public class AnalyticsIntegrationTests : IClassFixture<BarberSalonWebFactory>
{
    private readonly HttpClient _client;

    public AnalyticsIntegrationTests(BarberSalonWebFactory factory)
        => _client = factory.GetClient();

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task GetRevenueForecast_Returns200WithEnvelope()
    {
        var response = await _client.GetAsync("/api/v1/dashboard/revenue-forecast");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content
            .ReadFromJsonAsync<ApiEnvelope<RevenueForecastDto>>(JsonOptions);
        envelope!.Success.Should().BeTrue();
        envelope.Data.DailyBreakdown.Should().HaveCount(7);
        envelope.Data.TotalExpected.Should().Be(envelope.Data.ConfirmedRevenue + envelope.Data.PendingRevenue);
    }

    [Fact]
    public async Task GetAtRiskCustomers_Returns200WithList()
    {
        var response = await _client.GetAsync("/api/v1/dashboard/at-risk-customers");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content
            .ReadFromJsonAsync<ApiEnvelope<List<AtRiskCustomerDto>>>(JsonOptions);
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetGapAnalysis_Returns200WithList()
    {
        var response = await _client.GetAsync("/api/v1/dashboard/gap-analysis?maxGaps=5");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content
            .ReadFromJsonAsync<ApiEnvelope<List<TimeGapDto>>>(JsonOptions);
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
        envelope.Data.Should().HaveCountLessThanOrEqualTo(5);
    }

    private sealed record ApiEnvelope<T>(T Data, bool Success, string Message);
}
