using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BarberSalon.Application.Booking.DTOs;
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

namespace BarberSalon.IntegrationTests.Booking;

public class GetAppointmentsByDateIntegrationTests : IClassFixture<BarberSalonWebFactory>
{
    private const string Endpoint = "/api/v1/appointments";

    private readonly HttpClient _client;
    private readonly BarberSalonWebFactory _factory;

    public GetAppointmentsByDateIntegrationTests(BarberSalonWebFactory factory)
    {
        _factory = factory;
        _client = factory.GetClient();
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task GetAppointments_WithDateQueryParam_WhenAppointmentsExist_Returns200WithMatchingAppointmentsOnly()
    {
        // Arrange
        var targetDate = new DateOnly(2026, 9, 15);
        var targetDateString = "2026-09-15";
        var otherDate = new DateOnly(2026, 9, 16);

        Guid appointmentId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var user = User.Create("09121110001", DateTime.UtcNow, UserRole.Customer, "مشتری هدف");
            var staff = StaffMember.Create("استایلیست یک", "stylist-one", "09121110002", "توضیح", "آرایشگر", 4);
            var service = SalonService.Create("کوتاهی کلاسیک", "کوتاهی تخصصی", 45, 180000m, "haircut");

            db.Users.Add(user);
            db.StaffMembers.Add(staff);
            db.SalonServices.Add(service);

            var slot1 = TimeSlot.Create(targetDate, new TimeOnly(10, 0), new TimeOnly(10, 45));
            var apt1 = Appointment.Create(user.Id, staff.Id, service.Id, slot1, 180000m, "نوبت هدف");

            var slot2 = TimeSlot.Create(otherDate, new TimeOnly(11, 0), new TimeOnly(11, 45));
            var apt2 = Appointment.Create(user.Id, staff.Id, service.Id, slot2, 180000m, "نوبت روز دیگر");

            db.Appointments.AddRange(apt1, apt2);
            await db.SaveChangesAsync();

            appointmentId = apt1.Id;
        }

        // Act
        var response = await _client.GetAsync($"{Endpoint}?date={targetDateString}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<List<AppointmentDto>>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
        envelope.Data.Should().Contain(a => a.Id == appointmentId && a.Date == targetDateString && a.CustomerName == "مشتری هدف");
        envelope.Data.Should().NotContain(a => a.Date == "2026-09-16");
    }

    [Fact]
    public async Task GetAppointments_WithDateRouteParam_WhenAppointmentsExist_Returns200WithMatchingAppointmentsOnly()
    {
        // Arrange
        var targetDate = new DateOnly(2026, 9, 20);
        var targetDateString = "2026-09-20";

        Guid appointmentId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var user = User.Create("09121110003", DateTime.UtcNow, UserRole.Customer, "مشتری روت");
            var staff = StaffMember.Create("استایلیست دو", "stylist-two", "09121110004", "توضیح", "آرایشگر", 3);
            var service = SalonService.Create("اصلاح ریش", "اصلاح با تیغ", 30, 90000m, "beard");

            db.Users.Add(user);
            db.StaffMembers.Add(staff);
            db.SalonServices.Add(service);

            var slot = TimeSlot.Create(targetDate, new TimeOnly(14, 0), new TimeOnly(14, 30));
            var apt = Appointment.Create(user.Id, staff.Id, service.Id, slot, 90000m, "تست روت");

            db.Appointments.Add(apt);
            await db.SaveChangesAsync();

            appointmentId = apt.Id;
        }

        // Act
        var response = await _client.GetAsync($"{Endpoint}/date/{targetDateString}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<List<AppointmentDto>>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
        envelope.Data.Should().Contain(a => a.Id == appointmentId && a.Date == targetDateString && a.StaffName == "استایلیست دو");
    }

    [Fact]
    public async Task GetAppointments_WhenNoAppointmentsForDate_Returns200WithEmptyList()
    {
        // Act
        var response = await _client.GetAsync($"{Endpoint}?date=2099-12-31");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<List<AppointmentDto>>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
        envelope.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAppointments_WithInvalidDateFormatInQuery_Returns400BadRequest()
    {
        // Act
        var response = await _client.GetAsync($"{Endpoint}?date=invalid-date-format");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<object?>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeFalse();
        envelope.Message.Should().Contain("Invalid date format");
    }

    [Fact]
    public async Task GetAppointments_WithInvalidDateFormatInRoute_Returns400BadRequest()
    {
        // Act
        var response = await _client.GetAsync($"{Endpoint}/date/2026/09/01");

        // Assert (either routing 404 or controller validation 400 depending on route, testing standard invalid string)
        var responseInvalid = await _client.GetAsync($"{Endpoint}/date/not-a-date");
        responseInvalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var envelope = await responseInvalid.Content.ReadFromJsonAsync<ApiResponseEnvelope<object?>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeFalse();
        envelope.Message.Should().Contain("Invalid date format");
    }

    private sealed record ApiResponseEnvelope<T>(T Data, bool Success, string Message);
}
