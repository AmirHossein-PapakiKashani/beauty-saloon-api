using System.Net;
using System.Net.Http.Json;
using BarberSalon.API.Common;
using BarberSalon.Application.Staff.DTOs;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Xunit;

namespace BarberSalon.IntegrationTests.Staff;

public sealed class StaffSlugIntegrationTests : IClassFixture<BarberSalonWebFactory>
{
    private readonly HttpClient _client;

    public StaffSlugIntegrationTests(BarberSalonWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_WithMatchingSlug_Returns200AndMatchingStaffMember()
    {
        // Arrange
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var slug = $"barber-{suffix}";
        var request = new CreateStaffRequest(
            $"علی کریمی {suffix}",
            slug,
            $"0912{Random.Shared.Next(1000000, 9999999)}",
            "استاد کوتاهی",
            "آرایشگر ارشد",
            7,
            new List<string> { "Haircut", "Fade" },
            new List<Guid>(),
            null,
            null);

        var createRes = await _client.PostAsJsonAsync("/api/v1/staff", request);
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act
        var response = await _client.GetAsync($"/api/v1/staff?slug={slug}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<StaffDto>>();
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
        envelope.Data.Slug.Should().Be(slug);
        envelope.Data.Name.Should().Be($"علی کریمی {suffix}");
    }

    [Fact]
    public async Task GetAll_WithNonExistentSlug_Returns404NotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/staff?slug=non-existent-slug-xyz-999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
