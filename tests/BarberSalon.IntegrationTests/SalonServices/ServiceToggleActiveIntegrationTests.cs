using System.Net;
using System.Net.Http.Json;
using BarberSalon.API.Common;
using BarberSalon.Application.SalonServices.DTOs;
using BarberSalon.Domain.SalonServices.Entities;
using BarberSalon.Infrastructure.Persistence;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BarberSalon.IntegrationTests.SalonServices;

public sealed class ServiceToggleActiveIntegrationTests : IClassFixture<BarberSalonWebFactory>
{
    private readonly HttpClient _client;
    private readonly BarberSalonWebFactory _factory;

    public ServiceToggleActiveIntegrationTests(BarberSalonWebFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PutService_WithIsActiveFalse_TogglesOffAndPersists()
    {
        // Arrange
        Guid serviceId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var s = SalonService.Create("کوتاهی تست ضامن", 30, 200000, "Hair");
            db.SalonServices.Add(s);
            await db.SaveChangesAsync();
            serviceId = s.Id;
        }

        var updatePayload = new
        {
            name = "کوتاهی تست ضامن",
            description = "توضیح",
            durationMinutes = 30,
            price = 200000,
            category = "Hair",
            isActive = false
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/salon-services/{serviceId}", updatePayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<SalonServiceDto>>();
        envelope.Should().NotBeNull();
        envelope!.Data.IsActive.Should().BeFalse();

        // Verify in DB
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var dbService = await db.SalonServices.FirstOrDefaultAsync(x => x.Id == serviceId);
            dbService.Should().NotBeNull();
            dbService!.IsActive.Should().BeFalse();
        }
    }

    [Fact]
    public async Task PutService_WithIsActiveTrue_ReactivatesServiceAndPersists()
    {
        // Arrange
        Guid serviceId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var s = SalonService.Create("کوتاهی غیرفعال", 45, 250000, "Hair");
            s.Archive();
            db.SalonServices.Add(s);
            await db.SaveChangesAsync();
            serviceId = s.Id;
        }

        var updatePayload = new
        {
            name = "کوتاهی غیرفعال",
            description = "توضیح مجدد",
            durationMinutes = 45,
            price = 250000,
            category = "Hair",
            isActive = true
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/salon-services/{serviceId}", updatePayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<SalonServiceDto>>();
        envelope.Should().NotBeNull();
        envelope!.Data.IsActive.Should().BeTrue();

        // Verify in DB
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var dbService = await db.SalonServices.FirstOrDefaultAsync(x => x.Id == serviceId);
            dbService.Should().NotBeNull();
            dbService!.IsActive.Should().BeTrue();
        }
    }
}
