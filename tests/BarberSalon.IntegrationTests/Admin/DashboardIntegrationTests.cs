using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BarberSalon.Application.Admin.DTOs;
using BarberSalon.Application.Common.Interfaces;
using BarberSalon.Domain.Auth.Entities;
using BarberSalon.Domain.Auth.Enums;
using BarberSalon.Domain.Booking.Entities;
using BarberSalon.Domain.Booking.ValueObjects;
using BarberSalon.Domain.SalonServices.Entities;
using BarberSalon.Domain.Staff.Entities;
using BarberSalon.Infrastructure.Persistence;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BarberSalon.IntegrationTests.Admin;

public class DashboardIntegrationTests : IClassFixture<BarberSalonWebFactory>
{
    private const string Endpoint = "/api/v1/dashboard/stats";

    private readonly HttpClient _client;
    private readonly BarberSalonWebFactory _factory;

    public DashboardIntegrationTests(BarberSalonWebFactory factory)
    {
        _factory = factory;
        _client = factory.GetClient();
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task GetDashboardStats_WhenAppointmentsExist_Returns200WithAccurateCalculatedMetrics()
    {
        // Arrange
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var clock = scope.ServiceProvider.GetRequiredService<IClock>();
            var today = DateOnly.FromDateTime(clock.UtcNow);

            var user1 = User.Create("09120000001", DateTime.UtcNow, UserRole.Customer, "مشتری یک");
            var user2 = User.Create("09120000002", DateTime.UtcNow, UserRole.Customer, "مشتری دو");
            var user3 = User.Create("09120000003", DateTime.UtcNow, UserRole.Customer, "مشتری سه");
            var user4 = User.Create("09120000004", DateTime.UtcNow, UserRole.Customer, "مشتری چهار");

            var staff = StaffMember.Create("آرایشگر اصلی", "main-stylist", "09120000099", "بیو", "آرایشگر", 5);
            var service = SalonService.Create("اصلاح ویژه", "شرح", 30, 200000m, "haircut");

            db.Users.AddRange(user1, user2, user3, user4);
            db.StaffMembers.Add(staff);
            db.SalonServices.Add(service);

            // 1. Today Pending (Price 200000, user1)
            var apt1 = Appointment.Create(user1.Id, staff.Id, service.Id, TimeSlot.Create(today, new TimeOnly(9, 0), new TimeOnly(9, 30)), 200000m);

            // 2. Today Confirmed (Price 300000, user2)
            var apt2 = Appointment.Create(user2.Id, staff.Id, service.Id, TimeSlot.Create(today, new TimeOnly(10, 0), new TimeOnly(10, 30)), 300000m);
            apt2.Confirm();

            // 3. Today Completed (Price 500000, user3)
            var apt3 = Appointment.Create(user3.Id, staff.Id, service.Id, TimeSlot.Create(today, new TimeOnly(11, 0), new TimeOnly(11, 30)), 500000m);
            apt3.Confirm();
            apt3.Complete();

            // 4. Today Cancelled (Price 400000, user4)
            var apt4 = Appointment.Create(user4.Id, staff.Id, service.Id, TimeSlot.Create(today, new TimeOnly(12, 0), new TimeOnly(12, 30)), 400000m);
            apt4.Cancel("Cancelled");

            // 5. Past 2 days ago Confirmed (user1, in weekly window)
            var apt5 = Appointment.Create(user1.Id, staff.Id, service.Id, TimeSlot.Create(today.AddDays(-2), new TimeOnly(14, 0), new TimeOnly(14, 30)), 150000m);
            apt5.Confirm();

            // 6. Future 3 days ahead Pending (user2)
            var apt6 = Appointment.Create(user2.Id, staff.Id, service.Id, TimeSlot.Create(today.AddDays(3), new TimeOnly(15, 0), new TimeOnly(15, 30)), 250000m);

            db.Appointments.AddRange(apt1, apt2, apt3, apt4, apt5, apt6);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync(Endpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<DashboardStatsEnvelope>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Message.Should().NotBeNullOrWhiteSpace();

        var data = envelope.Data;
        data.Should().NotBeNull();
        // Today active = apt1(pending) + apt2(confirmed) + apt3(completed) = 3 (apt4 is cancelled)
        data.TodayAppointments.Should().Be(3);
        // Pending = apt1 (today) + apt6 (future) = 2
        data.PendingAppointments.Should().Be(2);
        // Weekly distinct active customers in [today - 6, today] = user1, user2, user3 = 3
        data.WeeklyCustomers.Should().Be(3);
        // Today revenue from confirmed (300k) + completed (500k) = 800k
        data.TodayRevenue.Should().Be(800000m);
    }

    [Theory]
    [InlineData("/api/v1/dashboard/stats")]
    [InlineData("/api/v1/dashboard")]
    [InlineData("/api/v1/dashboard-stats")]
    [InlineData("/api/v1/admin/dashboard-stats")]
    public async Task GetDashboardStats_ViaSupportedRoutes_Returns200(string route)
    {
        // Act
        var response = await _client.GetAsync(route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<DashboardStatsEnvelope>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
    }

    private sealed record DashboardStatsEnvelope(DashboardStatsDto Data, bool Success, string Message);
}
