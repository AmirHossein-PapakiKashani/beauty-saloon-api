using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BarberSalon.Application.Portfolio.DTOs;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Xunit;

namespace BarberSalon.IntegrationTests.Portfolio;

public class PortfolioActionsIntegrationTests : IClassFixture<BarberSalonWebFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public PortfolioActionsIntegrationTests(BarberSalonWebFactory factory)
    {
        _client = factory.GetClient();
    }

    [Fact]
    public async Task LikeAndTogglePortfolioItem_ExecutesSuccessfully()
    {
        // Arrange - create portfolio item
        var createReq = new CreatePortfolioItemRequest(
            "مدل مو کلاسیک",
            "haircut",
            "https://example.com/before.jpg",
            "https://example.com/after.jpg",
            "توضیحات تست",
            null
        );

        var createRes = await _client.PostAsJsonAsync("/api/v1/portfolio", createReq);
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createRes.Content.ReadFromJsonAsync<ApiResponseEnvelope<PortfolioItemDto>>(JsonOptions);
        var itemId = created!.Data.Id;

        // 1. Like item
        var likeRes = await _client.PostAsync($"/api/v1/portfolio/{itemId}/like", null);
        likeRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var liked = await likeRes.Content.ReadFromJsonAsync<ApiResponseEnvelope<PortfolioItemDto>>(JsonOptions);
        liked!.Data.LikesCount.Should().Be(1);

        // 2. Toggle Featured
        var featRes = await _client.PatchAsync($"/api/v1/portfolio/{itemId}/toggle-featured", null);
        featRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var featured = await featRes.Content.ReadFromJsonAsync<ApiResponseEnvelope<PortfolioItemDto>>(JsonOptions);
        featured!.Data.IsFeatured.Should().BeTrue();

        // 3. Toggle Published
        var pubRes = await _client.PatchAsync($"/api/v1/portfolio/{itemId}/toggle-publish", null);
        pubRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var published = await pubRes.Content.ReadFromJsonAsync<ApiResponseEnvelope<PortfolioItemDto>>(JsonOptions);
        published!.Data.IsPublished.Should().BeFalse();
    }

    private sealed record ApiResponseEnvelope<T>(T Data, bool Success, string Message);
}
