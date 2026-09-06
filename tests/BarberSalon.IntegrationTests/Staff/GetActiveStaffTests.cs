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

public class GetActiveStaffTests : IClassFixture<BarberSalonWebFactory>
{
    private const string Endpoint = "/api/v1/staff";

    private readonly HttpClient _client;
    private readonly BarberSalonWebFactory _factory;

    public GetActiveStaffTests(BarberSalonWebFactory factory)
    {
        _factory = factory;
        _client = factory.GetClient();
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task GetActiveStaff_WhenStaffExist_Returns200WithActiveStaffOnly()
    {
        // Arrange
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var activeStaff1 = StaffMember.Create("آقای کریمی", "karimi-test1", "09121234567", "آرایشگر ارشد", "آرایشگر ارشد", 10);
            var activeStaff2 = StaffMember.Create("خانم احمدی", "ahmadi-test1", "09129876543", "متخصص رنگ", "متخصص رنگ", 8);
            var archivedStaff = StaffMember.Create("آقای جعفری", "jafari-test1", "09125556677", "آرایشگر", "آرایشگر", 3);
            archivedStaff.Archive();

            db.StaffMembers.AddRange(activeStaff1, activeStaff2, archivedStaff);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync(Endpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<List<StaffDto>>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
        envelope.Data.Should().Contain(s => s.Slug == "karimi-test1" && s.IsActive);
        envelope.Data.Should().Contain(s => s.Slug == "ahmadi-test1" && s.IsActive);
        envelope.Data.Should().NotContain(s => s.Slug == "jafari-test1");
    }

    [Fact]
    public async Task GetActiveStaff_WhenNoActiveStaff_Returns200WithEmptyList()
    {
        // Act
        var response = await _client.GetAsync(Endpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<List<StaffDto>>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task GetActiveStaff_WithStatusActiveQuery_Returns200WithActiveStaff()
    {
        // Arrange
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var staff = StaffMember.Create("آقای رضایی", "rezaei-test1", "09123456789", "کوتاهی مردانه", "آرایشگر", 6);
            db.StaffMembers.Add(staff);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync($"{Endpoint}?status=active");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<List<StaffDto>>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().Contain(s => s.Slug == "rezaei-test1");
    }

    [Fact]
    public async Task GetActiveStaff_WithServiceIdQuery_Returns200WithMatchingStaff()
    {
        // Arrange
        var serviceId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var staffWithService = StaffMember.Create("خانم محمدی", "mohammadi-test1", "09127778899", "متخصص کراتین", "متخصص کراتین", 7, serviceIds: new List<Guid> { serviceId });
            var staffWithoutService = StaffMember.Create("آقای نادری", "naderi-test1", "09123334455", "آرایشگر", "آرایشگر", 2);

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
        envelope.Data.Should().Contain(s => s.Slug == "mohammadi-test1");
        envelope.Data.Should().NotContain(s => s.Slug == "naderi-test1");
    }

    [Fact]
    public async Task GetActiveStaff_WithServiceIdQuery_IgnoresArchivedStaffWithService()
    {
        // Arrange
        var serviceId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var archivedStaffWithService = StaffMember.Create("آقای کریمی", "karimi-archived-test1", "09121234567", "آرایشگر", "آرایشگر", 10, serviceIds: new List<Guid> { serviceId });
            archivedStaffWithService.Archive();
            var activeStaffWithService = StaffMember.Create("خانم احمدی", "ahmadi-test1", "09129876543", "متخصص رنگ", "متخصص رنگ", 8, serviceIds: new List<Guid> { serviceId });

            db.StaffMembers.AddRange(archivedStaffWithService, activeStaffWithService);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync($"{Endpoint}?serviceId={serviceId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<List<StaffDto>>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().Contain(s => s.Slug == "ahmadi-test1");
        envelope.Data.Should().NotContain(s => s.Slug == "karimi-archived-test1");
    }

    [Fact]
    public async Task GetActiveStaff_WithServiceIdQuery_WhenNoStaffMatch_ReturnsEmptyList()
    {
        // Arrange
        var serviceId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"{Endpoint}?serviceId={serviceId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<List<StaffDto>>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
        envelope.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetById_WhenStaffExists_Returns200WithStaff()
    {
        // Arrange
        Guid staffId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var staff = StaffMember.Create("آقای بیات", "bayat-test1", "09129990011", "آرایشگر مجرب", "آرایشگر", 4);
            db.StaffMembers.Add(staff);
            await db.SaveChangesAsync();
            staffId = staff.Id;
        }

        // Act
        var response = await _client.GetAsync($"{Endpoint}/{staffId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<StaffDto>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
        envelope.Data!.Id.Should().Be(staffId);
        envelope.Data.Name.Should().Be("آقای بیات");
        envelope.Data.Slug.Should().Be("bayat-test1");
    }

    private sealed record ApiResponseEnvelope<T>(T Data, bool Success, string Message);
}
