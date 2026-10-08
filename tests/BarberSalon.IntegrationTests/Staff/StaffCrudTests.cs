using System.Net;
using System.Net.Http.Json;
using BarberSalon.API.Common;
using BarberSalon.Application.Staff.DTOs;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Xunit;

namespace BarberSalon.IntegrationTests.Staff;

public sealed class StaffCrudTests : IClassFixture<BarberSalonWebFactory>
{
    private readonly HttpClient _client;

    public StaffCrudTests(BarberSalonWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_WithValidData_CreatesStaff()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var request = new CreateStaffRequest(
            $"Staff {suffix}",
            $"staff-{suffix}",
            $"0912{Random.Shared.Next(1000000, 9999999)}",
            "Expert Barber",
            "Master Barber",
            5,
            new List<string> { "Haircut", "Fade" },
            new List<Guid>(),
            null,
            null);

        var response = await _client.PostAsJsonAsync("/api/v1/staff", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<StaffDto>>();
        body.Should().NotBeNull();
        body!.Data.Slug.Should().Be($"staff-{suffix}");
    }

    [Fact]
    public async Task Put_WithValidData_UpdatesStaff()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var createRequest = new CreateStaffRequest(
            $"Staff {suffix}",
            $"staff-{suffix}",
            $"0912{Random.Shared.Next(1000000, 9999999)}",
            "Bio",
            "Barber",
            3,
            new List<string> { "Beard" },
            new List<Guid>(),
            null,
            null);
        var createRes = await _client.PostAsJsonAsync("/api/v1/staff", createRequest);
        var created = (await createRes.Content.ReadFromJsonAsync<ApiResponse<StaffDto>>())!.Data;

        var updateRequest = new UpdateStaffRequest(
            $"Staff {suffix} Updated",
            $"staff-{suffix}",
            created.Phone,
            "Updated Bio",
            "Senior Barber",
            4,
            new List<string> { "Beard", "Styling" },
            new List<Guid>(),
            null,
            null);

        var updateRes = await _client.PutAsJsonAsync($"/api/v1/staff/{created.Id}", updateRequest);

        updateRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = (await updateRes.Content.ReadFromJsonAsync<ApiResponse<StaffDto>>())!.Data;
        updated.Name.Should().Be($"Staff {suffix} Updated");
        updated.Role.Should().Be("Senior Barber");
    }

    [Fact]
    public async Task Delete_WithExistingId_ArchivesStaff()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var createRequest = new CreateStaffRequest(
            $"Staff {suffix}",
            $"staff-{suffix}",
            $"0912{Random.Shared.Next(1000000, 9999999)}",
            "Bio",
            "Barber",
            2,
            new List<string>(),
            new List<Guid>(),
            null,
            null);
        var createRes = await _client.PostAsJsonAsync("/api/v1/staff", createRequest);
        var created = (await createRes.Content.ReadFromJsonAsync<ApiResponse<StaffDto>>())!.Data;

        var deleteRes = await _client.DeleteAsync($"/api/v1/staff/{created.Id}");

        deleteRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Post_Activate_WithArchivedId_ReactivatesStaff()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var createRequest = new CreateStaffRequest(
            $"Staff {suffix}",
            $"staff-{suffix}",
            $"0912{Random.Shared.Next(1000000, 9999999)}",
            "Bio",
            "Barber",
            2,
            new List<string>(),
            new List<Guid>(),
            null,
            null);
        var createRes = await _client.PostAsJsonAsync("/api/v1/staff", createRequest);
        var created = (await createRes.Content.ReadFromJsonAsync<ApiResponse<StaffDto>>())!.Data;

        // Archive first
        var deleteRes = await _client.DeleteAsync($"/api/v1/staff/{created.Id}");
        deleteRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Reactivate
        var activateRes = await _client.PostAsync($"/api/v1/staff/{created.Id}/activate", null);
        activateRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify it is active again
        var getRes = await _client.GetAsync($"/api/v1/staff/{created.Id}");
        var fetched = (await getRes.Content.ReadFromJsonAsync<ApiResponse<StaffDto>>())!.Data;
        fetched.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Post_WithServicePrices_PersistsAndReturnsServicePrices()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var serviceId = Guid.NewGuid();
        var prices = new Dictionary<Guid, decimal> { [serviceId] = 250000m };

        var request = new CreateStaffRequest(
            $"Staff {suffix}",
            $"staff-{suffix}",
            $"0912{Random.Shared.Next(1000000, 9999999)}",
            "Color Specialist",
            "Senior Colorist",
            8,
            new List<string> { "Color", "Balayage" },
            new List<Guid> { serviceId },
            null,
            null,
            prices);

        var response = await _client.PostAsJsonAsync("/api/v1/staff", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<StaffDto>>();
        body.Should().NotBeNull();
        body!.Data.ServicePrices.Should().NotBeNull();
        body.Data.ServicePrices.Should().ContainKey(serviceId);
        body.Data.ServicePrices![serviceId].Should().Be(250000m);

        // Fetch by ID to ensure it persisted in database
        var getRes = await _client.GetAsync($"/api/v1/staff/{body.Data.Id}");
        var fetched = (await getRes.Content.ReadFromJsonAsync<ApiResponse<StaffDto>>())!.Data;
        fetched.ServicePrices.Should().NotBeNull();
        fetched.ServicePrices.Should().ContainKey(serviceId);
        fetched.ServicePrices![serviceId].Should().Be(250000m);
    }
}

