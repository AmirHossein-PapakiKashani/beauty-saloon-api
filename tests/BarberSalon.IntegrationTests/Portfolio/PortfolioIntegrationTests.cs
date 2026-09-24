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

    [Fact]
    public async Task UpdatePortfolioItem_ReturnsUpdatedItem()
    {
        var createRequest = new CreatePortfolioItemRequest(
            "Original Title",
            "Beard",
            "https://images.unsplash.com/before.jpg",
            "https://images.unsplash.com/after.jpg",
            "Original description",
            null
        );

        var postResponse = await _client.PostAsJsonAsync("/api/v1/portfolio", createRequest);
        postResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await postResponse.Content.ReadFromJsonAsync<ApiResponseEnvelope<PortfolioItemDto>>(JsonOptions))!.Data;

        var updateRequest = new UpdatePortfolioItemRequest(
            "Updated Title",
            "Haircut",
            "https://images.unsplash.com/before-updated.jpg",
            "https://images.unsplash.com/after-updated.jpg",
            "Updated description",
            null
        );

        var putResponse = await _client.PutAsJsonAsync($"/api/v1/portfolio/{created.Id}", updateRequest);
        putResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedEnvelope = await putResponse.Content.ReadFromJsonAsync<ApiResponseEnvelope<PortfolioItemDto>>(JsonOptions);
        updatedEnvelope.Should().NotBeNull();
        updatedEnvelope!.Success.Should().BeTrue();
        updatedEnvelope.Data.Title.Should().Be("Updated Title");
        updatedEnvelope.Data.Category.Should().Be("Haircut");
        updatedEnvelope.Data.Description.Should().Be("Updated description");

        await _client.DeleteAsync($"/api/v1/portfolio/{created.Id}");
    }

    private sealed record ApiResponseEnvelope<T>(T Data, bool Success, string Message);
}
