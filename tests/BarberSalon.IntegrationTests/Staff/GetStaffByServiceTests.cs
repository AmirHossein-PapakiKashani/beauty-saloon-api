using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BarberSalon.Application.Staff.DTOs;
using BarberSalon.Domain.Staff.Entities;
using BarberSalon.Infrastructure.Persistence;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BarberSalon.IntegrationTests.Staff;

/// <summary>
/// End-to-end tests for the GetStaffByService use case (GET /api/v1/staff?serviceId=).
/// Returns only ACTIVE staff members who perform the given salon service.
/// </summary>
public class GetStaffByServiceTests : IClassFixture<BarberSalonWebFactory>
{
    private const string Endpoint = "/api/v1/staff";

    private readonly HttpClient _client;
    private readonly BarberSalonWebFactory _factory;

    public GetStaffByServiceTests(BarberSalonWebFactory factory)
    {
        _factory = factory;
        _client = factory.GetClient();
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task GetStaffByService_WhenActiveStaffPerformsService_ReturnsOnlyMatchingActiveStaff()
    {
        // Arrange
        var serviceId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var staffWithService = StaffMember.Create("خانم محمدی", "mohammadi-service1", "09127778899", "متخصص کراتین", "متخصص کراتین", 7, serviceIds: new List<Guid> { serviceId });
            var staffWithoutService = StaffMember.Create("آقای نادری", "naderi-service1", "09123334455", "آرایشگر", "آرایشگر", 2);

            db.StaffMembers.AddRange(staffWithService, staffWithoutService);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync($"{Endpoint}?serviceId={serviceId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<List<StaffDto>>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().Contain(s => s.Slug == "mohammadi-service1");
        envelope.Data.Should().NotContain(s => s.Slug == "naderi-service1");
    }

    [Fact]
    public async Task GetStaffByService_WhenArchivedStaffPerformsService_ExcludesArchivedStaff()
    {
        // Arrange
        var serviceId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var activeStaff = StaffMember.Create("آقای کریمی", "karimi-service1", "09121234567", "آرایشگر ارشد", "آرایشگر ارشد", 10, serviceIds: new List<Guid> { serviceId });
            var archivedStaff = StaffMember.Create("آقای جعفری", "jafari-service1", "09125556677", "آرایشگر", "آرایشگر", 3, serviceIds: new List<Guid> { serviceId });
            archivedStaff.Archive();

            db.StaffMembers.AddRange(activeStaff, archivedStaff);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync($"{Endpoint}?serviceId={serviceId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<List<StaffDto>>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().Contain(s => s.Slug == "karimi-service1");
        envelope.Data.Should().NotContain(s => s.Slug == "jafari-service1");
    }

    [Fact]
    public async Task GetStaffByService_WhenNoStaffPerformsService_ReturnsEmptyList()
    {
        // Arrange
        var serviceId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var staffWithoutService = StaffMember.Create("آقای رضایی", "rezaei-service1", "09123456789", "کوتاهی مردانه", "آرایشگر", 6);
            db.StaffMembers.Add(staffWithoutService);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync($"{Endpoint}?serviceId={serviceId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<List<StaffDto>>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().BeEmpty();
    }

    private sealed record ApiResponseEnvelope<T>(T Data, bool Success, string Message);
}