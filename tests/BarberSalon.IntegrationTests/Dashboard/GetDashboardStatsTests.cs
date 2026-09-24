using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BarberSalon.API.Common;
using BarberSalon.Application.Admin.DTOs;
using BarberSalon.Domain.Booking.Entities;
using BarberSalon.Domain.Booking.Enums;
using BarberSalon.Domain.Booking.ValueObjects;
using BarberSalon.Infrastructure.Persistence;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BarberSalon.IntegrationTests.Dashboard;

public class GetDashboardStatsTests : IClassFixture<BarberSalonWebFactory>
{
    private const string Endpoint = "/api/v1/dashboard/stats";
    private readonly HttpClient _client;
    private readonly BarberSalonWebFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public GetDashboardStatsTests(BarberSalonWebFactory factory)
    {
        _factory = factory;
        _client = factory.GetClient();
    }

    [Fact]
    public async Task GetDashboardStats_WhenCalled_Returns200WithStatsEnvelope()
    {
        // Arrange
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var slot = TimeSlot.Create(today, new TimeOnly(10, 0), new TimeOnly(10, 45));
            var appointment = Appointment.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                slot,
                150000m,
                "یادداشت تستی");
            appointment.Confirm();

            db.Appointments.Add(appointment);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync(Endpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<DashboardStatsDto>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
        envelope.Data.TodayAppointments.Should().BeGreaterThanOrEqualTo(1);
    }
}
