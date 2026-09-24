using System.Net;
using System.Net.Http.Json;
using BarberSalon.API.Common;
using BarberSalon.Application.SalonServices.DTOs;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Xunit;

namespace BarberSalon.IntegrationTests.SalonServices;

public sealed class SalonServicesCrudTests : IClassFixture<BarberSalonWebFactory>
{
    private readonly HttpClient _client;

    public SalonServicesCrudTests(BarberSalonWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_WithValidData_CreatesSalonService()
    {
        var uniqueName = $"Service_{Guid.NewGuid():N}";
        var request = new CreateSalonServiceRequest(uniqueName, "Testing description", 45, 120000m, "Hair");

        var response = await _client.PostAsJsonAsync("/api/v1/salon-services", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<SalonServiceDto>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Name.Should().Be(uniqueName);
        body.Data.Price.Should().Be(120000m);
    }

    [Fact]
    public async Task Put_WithValidData_UpdatesSalonService()
    {
        var uniqueName = $"Service_{Guid.NewGuid():N}";
        var createRequest = new CreateSalonServiceRequest(uniqueName, "Old desc", 30, 80000m, "Beard");
        var createResponse = await _client.PostAsJsonAsync("/api/v1/salon-services", createRequest);
        var created = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<SalonServiceDto>>())!.Data;

        var updateRequest = new UpdateSalonServiceRequest($"{uniqueName}_Updated", "New desc", 50, 95000m, "Beard");
        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/salon-services/{created.Id}", updateRequest);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = (await updateResponse.Content.ReadFromJsonAsync<ApiResponse<SalonServiceDto>>())!.Data;
        updated.Name.Should().Be($"{uniqueName}_Updated");
        updated.DurationMinutes.Should().Be(50);
    }

    [Fact]
    public async Task Delete_WithExistingId_ArchivesService()
    {
        var uniqueName = $"Service_{Guid.NewGuid():N}";
        var createRequest = new CreateSalonServiceRequest(uniqueName, "To delete", 30, 80000m, "Hair");
        var createResponse = await _client.PostAsJsonAsync("/api/v1/salon-services", createRequest);
        var created = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<SalonServiceDto>>())!.Data;

        var deleteResponse = await _client.DeleteAsync($"/api/v1/salon-services/{created.Id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
