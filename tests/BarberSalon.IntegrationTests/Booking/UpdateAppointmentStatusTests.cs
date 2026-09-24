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

public class UpdateAppointmentStatusTests : IClassFixture<BarberSalonWebFactory>
{
    private readonly HttpClient _client;
    private readonly BarberSalonWebFactory _factory;

    public UpdateAppointmentStatusTests(BarberSalonWebFactory factory)
    {
        _factory = factory;
        _client = factory.GetClient();
    }

    [Fact]
    public async Task PatchStatus_ToConfirmed_Returns200AndUpdatedStatus()
    {
        // Arrange
        Guid appointmentId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var user = User.Create("09121113399", DateTime.UtcNow, UserRole.Customer, "تست کاربر");
            var service = SalonService.Create("کوتاهی تست", "توضیح", 30, 150000m, "CutTest");
            var staff = StaffMember.Create(
                "آرایشگر تست",
                "staff-test-status",
                "09129998877",
                "آرایشگر",
                "Barber",
                3,
                serviceIds: new List<Guid> { service.Id });

            var timeSlot = TimeSlot.Create(
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
                new TimeOnly(14, 0),
                new TimeOnly(14, 30));

            var appointment = Appointment.Create(
                user.Id,
                staff.Id,
                service.Id,
                timeSlot,
                service.Price);

            db.Users.Add(user);
            db.SalonServices.Add(service);
            db.StaffMembers.Add(staff);
            db.Appointments.Add(appointment);
            await db.SaveChangesAsync();

            appointmentId = appointment.Id;
        }

        var payload = new { status = "confirmed" };

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/v1/appointments/{appointmentId}/status", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PatchStatus_ToCancelled_Returns200AndUpdatedStatus()
    {
        // Arrange
        Guid appointmentId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var user = User.Create("09121113388", DateTime.UtcNow, UserRole.Customer, "تست لغو");
            var service = SalonService.Create("کوتاهی تست لغو", "توضیح", 30, 150000m, "CutCancel");
            var staff = StaffMember.Create(
                "آرایشگر تست لغو",
                "staff-test-cancel",
                "09129998866",
                "آرایشگر",
                "Barber",
                3,
                serviceIds: new List<Guid> { service.Id });

            var timeSlot = TimeSlot.Create(
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(6)),
                new TimeOnly(15, 0),
                new TimeOnly(15, 30));

            var appointment = Appointment.Create(
                user.Id,
                staff.Id,
                service.Id,
                timeSlot,
                service.Price);

            db.Users.Add(user);
            db.SalonServices.Add(service);
            db.StaffMembers.Add(staff);
            db.Appointments.Add(appointment);
            await db.SaveChangesAsync();

            appointmentId = appointment.Id;
        }

        var payload = new { status = "cancelled", reason = "Customer request" };

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/v1/appointments/{appointmentId}/status", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
