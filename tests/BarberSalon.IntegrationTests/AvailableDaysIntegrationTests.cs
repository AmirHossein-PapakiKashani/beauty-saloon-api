using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;

namespace BarberSalon.IntegrationTests;

public class AvailableDaysIntegrationTests : IClassFixture<BarberSalonWebFactory>
{
    private const string Endpoint = "/api/v1/available-days";

    private readonly HttpClient _client;

    public AvailableDaysIntegrationTests(BarberSalonWebFactory factory)
    {
        _client = factory.GetClient();
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task GetAvailableDays_WithCount_ReturnsSuccessEnvelopeWithThatManyDays()
    {
        var response = await _client.GetAsync($"{Endpoint}?count=3");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<AvailableDaysEnvelope>(JsonOptions);

        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Message.Should().NotBeNull();
        envelope.Data.Should().HaveCount(3);
        envelope.Data.Should().OnlyContain(
            d => !string.IsNullOrWhiteSpace(d.FullDate) && !string.IsNullOrWhiteSpace(d.DayName));
    }

    [Fact]
    public async Task GetAvailableDays_WithoutCount_DefaultsToSevenDays()
    {
        var response = await _client.GetAsync(Endpoint);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<AvailableDaysEnvelope>(JsonOptions);

        envelope!.Data.Should().HaveCount(7);
    }

    [Fact]
    public async Task GetAvailableDays_ReturnsConsecutiveAscendingDates()
    {
        var response = await _client.GetAsync($"{Endpoint}?count=4");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<AvailableDaysEnvelope>(JsonOptions);
        envelope.Should().NotBeNull();

        var dates = envelope!.Data.Select(d => DateOnly.Parse(d.FullDate)).ToArray();
        dates.Should().BeInAscendingOrder();
        for (var i = 1; i < dates.Length; i++)
        {
            dates[i].Should().Be(dates[i - 1].AddDays(1));
        }
    }

    private sealed record AvailableDayEnvelopeDto(string Date, string DayName, string FullDate);

    private sealed record AvailableDaysEnvelope(List<AvailableDayEnvelopeDto> Data, bool Success, string Message);
}