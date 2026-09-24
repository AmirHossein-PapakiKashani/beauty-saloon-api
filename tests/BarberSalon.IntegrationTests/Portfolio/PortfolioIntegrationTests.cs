using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BarberSalon.Application.Portfolio.DTOs;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Xunit;

namespace BarberSalon.IntegrationTests.Portfolio;

public class PortfolioIntegrationTests : IClassFixture<BarberSalonWebFactory>
{
    private readonly HttpClient _client;
    private readonly BarberSalonWebFactory _factory;

    public PortfolioIntegrationTests(BarberSalonWebFactory factory)
    {
        _factory = factory;
        _client = factory.GetClient();
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task GetPortfolio_Returns200WithList()
    {
        var response = await _client.GetAsync("/api/v1/portfolio");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<List<PortfolioItemDto>>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAndDeletePortfolioItem_ExecutesSuccessfully()
    {
        var createRequest = new CreatePortfolioItemRequest(
            "Classic Fade & Beard",
            "Haircut",
            "https://images.unsplash.com/before.jpg",
            "https://images.unsplash.com/after.jpg",
            "Full styling and beard trim",
            Guid.NewGuid()
        );

        var postResponse = await _client.PostAsJsonAsync("/api/v1/portfolio", createRequest);
        postResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var envelope = await postResponse.Content.ReadFromJsonAsync<ApiResponseEnvelope<PortfolioItemDto>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Title.Should().Be("Classic Fade & Beard");
        envelope.Data.Category.Should().Be("Haircut");

        var itemId = envelope.Data.Id;

        var deleteResponse = await _client.DeleteAsync($"/api/v1/portfolio/{itemId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed record ApiResponseEnvelope<T>(T Data, bool Success, string Message);
}
