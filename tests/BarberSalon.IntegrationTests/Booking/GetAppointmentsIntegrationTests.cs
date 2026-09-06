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

public class GetAppointmentsIntegrationTests : IClassFixture<BarberSalonWebFactory>
{
    private const string Endpoint = "/api/v1/appointments";

    private readonly HttpClient _client;
    private readonly BarberSalonWebFactory _factory;

    public GetAppointmentsIntegrationTests(BarberSalonWebFactory factory)
    {
        _factory = factory;
        _client = factory.GetClient();
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task GetAppointments_WithoutQueryParams_WhenAppointmentsExist_Returns200WithAllAppointments()
    {
        // Arrange
        Guid apt1Id;
        Guid apt2Id;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var user1 = User.Create("09121115501", DateTime.UtcNow, UserRole.Customer, "مشتری همگانی یک");
            var user2 = User.Create("09121115502", DateTime.UtcNow, UserRole.Customer, "مشتری همگانی دو");

            var staff1 = StaffMember.Create("استایلیست همگانی", "stylist-all-1", "09121115503", "توضیح", "آرایشگر", 4);
            var service1 = SalonService.Create("کوتاهی مدرن", "کوتاهی حرفه‌ای", 40, 220000m, "haircut");

            db.Users.AddRange(user1, user2);
            db.StaffMembers.Add(staff1);
            db.SalonServices.Add(service1);

            var slot1 = TimeSlot.Create(new DateOnly(2026, 9, 25), new TimeOnly(11, 0), new TimeOnly(11, 40));
            var apt1 = Appointment.Create(user1.Id, staff1.Id, service1.Id, slot1, 220000m, "نوبت عمومی یک");

            var slot2 = TimeSlot.Create(new DateOnly(2026, 9, 26), new TimeOnly(15, 0), new TimeOnly(15, 40));
            var apt2 = Appointment.Create(user2.Id, staff1.Id, service1.Id, slot2, 220000m, "نوبت عمومی دو");

            db.Appointments.AddRange(apt1, apt2);
            await db.SaveChangesAsync();

            apt1Id = apt1.Id;
            apt2Id = apt2.Id;
        }

        // Act
        var response = await _client.GetAsync(Endpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<List<AppointmentDto>>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Message.Should().Be("Appointments retrieved successfully.");
        envelope.Data.Should().NotBeNull();

        envelope.Data.Should().Contain(a => a.Id == apt1Id && a.CustomerName == "مشتری همگانی یک" && a.StaffName == "استایلیست همگانی");
        envelope.Data.Should().Contain(a => a.Id == apt2Id && a.CustomerName == "مشتری همگانی دو" && a.StaffName == "استایلیست همگانی");
    }

    [Fact]
    public async Task GetAppointments_WithCustomerIdQueryParam_Returns200WithCustomerSpecificAppointments()
    {
        // Arrange
        Guid targetCustomerId;
        Guid otherCustomerId;
        Guid targetAppointmentId;
        Guid otherAppointmentId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var userTarget = User.Create("09121116601", DateTime.UtcNow, UserRole.Customer, "مشتری انتخابی");
            var userOther = User.Create("09121116602", DateTime.UtcNow, UserRole.Customer, "مشتری دیگر");

            var staff = StaffMember.Create("استایلیست فیلتر", "stylist-filter", "09121116603", "", "آرایشگر", 2);
            var service = SalonService.Create("سرویس فیلتر", "توضیح", 30, 150000m, "haircut");

            db.Users.AddRange(userTarget, userOther);
            db.StaffMembers.Add(staff);
            db.SalonServices.Add(service);

            var slot1 = TimeSlot.Create(new DateOnly(2026, 9, 28), new TimeOnly(10, 0), new TimeOnly(10, 30));
            var aptTarget = Appointment.Create(userTarget.Id, staff.Id, service.Id, slot1, 150000m);

            var slot2 = TimeSlot.Create(new DateOnly(2026, 9, 28), new TimeOnly(12, 0), new TimeOnly(12, 30));
            var aptOther = Appointment.Create(userOther.Id, staff.Id, service.Id, slot2, 150000m);

            db.Appointments.AddRange(aptTarget, aptOther);
            await db.SaveChangesAsync();

            targetCustomerId = userTarget.Id;
            otherCustomerId = userOther.Id;
            targetAppointmentId = aptTarget.Id;
            otherAppointmentId = aptOther.Id;
        }

        // Act
        var response = await _client.GetAsync($"{Endpoint}?customerId={targetCustomerId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<List<AppointmentDto>>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
        envelope.Data.Should().Contain(a => a.Id == targetAppointmentId && a.CustomerId == targetCustomerId);
        envelope.Data.Should().NotContain(a => a.Id == otherAppointmentId);
    }

    [Fact]
    public async Task GetAppointments_WithDateQueryParam_Returns200WithDateSpecificAppointments()
    {
        // Arrange
        var targetDate = new DateOnly(2026, 9, 30);
        var targetDateString = "2026-09-30";
        var otherDate = new DateOnly(2026, 9, 29);

        Guid targetAppointmentId;
        Guid otherAppointmentId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var user = User.Create("09121117701", DateTime.UtcNow, UserRole.Customer, "مشتری تاریخ");
            var staff = StaffMember.Create("استایلیست تاریخ", "stylist-date", "09121117702", "", "آرایشگر", 3);
            var service = SalonService.Create("سرویس تاریخ", "توضیح", 30, 160000m, "beard");

            db.Users.Add(user);
            db.StaffMembers.Add(staff);
            db.SalonServices.Add(service);

            var slot1 = TimeSlot.Create(targetDate, new TimeOnly(13, 0), new TimeOnly(13, 30));
            var apt1 = Appointment.Create(user.Id, staff.Id, service.Id, slot1, 160000m);

            var slot2 = TimeSlot.Create(otherDate, new TimeOnly(13, 0), new TimeOnly(13, 30));
            var apt2 = Appointment.Create(user.Id, staff.Id, service.Id, slot2, 160000m);

            db.Appointments.AddRange(apt1, apt2);
            await db.SaveChangesAsync();

            targetAppointmentId = apt1.Id;
            otherAppointmentId = apt2.Id;
        }

        // Act
        var response = await _client.GetAsync($"{Endpoint}?date={targetDateString}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<List<AppointmentDto>>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
        envelope.Data.Should().Contain(a => a.Id == targetAppointmentId && a.Date == targetDateString);
        envelope.Data.Should().NotContain(a => a.Id == otherAppointmentId);
    }

    private sealed record ApiResponseEnvelope<T>(T Data, bool Success, string Message);
}
