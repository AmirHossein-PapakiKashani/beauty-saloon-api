using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BarberSalon.Application.SalonServices.DTOs;
using BarberSalon.Domain.SalonServices.Entities;
using BarberSalon.Infrastructure.Persistence;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BarberSalon.IntegrationTests.SalonServices;

public class GetActiveSalonServicesTests : IClassFixture<BarberSalonWebFactory>
{
    private const string Endpoint = "/api/v1/salon-services";

    private readonly HttpClient _client;
    private readonly BarberSalonWebFactory _factory;

    public GetActiveSalonServicesTests(BarberSalonWebFactory factory)
    {
        _factory = factory;
        _client = factory.GetClient();
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task GetActiveSalonServices_WhenServicesExist_Returns200WithActiveServicesOnly()
    {
        // Arrange
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var activeService1 = SalonService.Create("کوتاهی مو", "کوتاهی حرفه‌ای", 45, 150000m, "haircut");
            var activeService2 = SalonService.Create("رنگ مو", "رنگ با بهترین مواد", 90, 350000m, "color");
            var archivedService = SalonService.Create("بوتاکس مو", "سرویس قدیمی", 90, 500000m, "keratin");
            archivedService.Archive();

            db.SalonServices.AddRange(activeService1, activeService2, archivedService);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync(Endpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<List<SalonServiceDto>>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
        envelope.Data.Should().Contain(s => s.Name == "کوتاهی مو" && s.IsActive);
        envelope.Data.Should().Contain(s => s.Name == "رنگ مو" && s.IsActive);
        envelope.Data.Should().NotContain(s => s.Name == "بوتاکس مو");
    }

    [Fact]
    public async Task GetActiveSalonServices_WhenNoActiveServices_Returns200WithEmptyList()
    {
        // Act
        var response = await _client.GetAsync(Endpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<List<SalonServiceDto>>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task GetActiveSalonServices_WithStatusActiveQuery_Returns200WithActiveServices()
    {
        // Arrange
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var service = SalonService.Create("براشینگ", "براشینگ تخصصی", 45, 120000m, "brushing");
            db.SalonServices.Add(service);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync($"{Endpoint}?status=active");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<List<SalonServiceDto>>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().Contain(s => s.Name == "براشینگ");
    }

    [Fact]
    public async Task GetById_WhenServiceExists_Returns200WithService()
    {
        // Arrange
        Guid serviceId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var service = SalonService.Create("مش فویلی", "مش فویلی تخصصی", 150, 600000m, "color");
            db.SalonServices.Add(service);
            await db.SaveChangesAsync();
            serviceId = service.Id;
        }

        // Act
        var response = await _client.GetAsync($"{Endpoint}/{serviceId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<SalonServiceDto>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
        envelope.Data!.Id.Should().Be(serviceId);
        envelope.Data.Name.Should().Be("مش فویلی");
        envelope.Data.Price.Should().Be(600000m);
    }

    private sealed record ApiResponseEnvelope<T>(T Data, bool Success, string Message);
}
