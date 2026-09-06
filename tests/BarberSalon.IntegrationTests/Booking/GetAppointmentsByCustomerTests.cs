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

public class GetAppointmentsByCustomerTests : IClassFixture<BarberSalonWebFactory>
{
    private const string Endpoint = "/api/v1/appointments";

    private readonly HttpClient _client;
    private readonly BarberSalonWebFactory _factory;

    public GetAppointmentsByCustomerTests(BarberSalonWebFactory factory)
    {
        _factory = factory;
        _client = factory.GetClient();
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static string GetFutureDateString(int daysAhead = 10)
    {
        return DateOnly.FromDateTime(DateTime.UtcNow.AddDays(daysAhead)).ToString("yyyy-MM-dd");
    }

    [Fact]
    public async Task GetAppointments_WithExistingCustomerIdAndAppointments_Returns200WithAppointments()
    {
        // Arrange
        Guid customerId;
        Guid staffId;
        Guid serviceId;
        var date1 = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));
        var date2 = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var user = User.Create("09121118899", DateTime.UtcNow, UserRole.Customer, "مهدی کاظمی");
            var service = SalonService.Create("کوتاهی کلاسیک", "اصلاح سر با ماشین و قیچی", 30, 250000m, "Haircut");
            var staff = StaffMember.Create("استاد حسینی", "hosseini-cust-test", "09129998877", "", "Barber", 7, serviceIds: new List<Guid> { service.Id });

            db.Users.Add(user);
            db.SalonServices.Add(service);
            db.StaffMembers.Add(staff);
            await db.SaveChangesAsync();

            customerId = user.Id;
            staffId = staff.Id;
            serviceId = service.Id;

            var slot1 = TimeSlot.Create(date1, new TimeOnly(11, 0), new TimeOnly(11, 30));
            var apt1 = Appointment.Create(customerId, staffId, serviceId, slot1, 250000m, "نوبت اول");

            var slot2 = TimeSlot.Create(date2, new TimeOnly(16, 0), new TimeOnly(16, 30));
            var apt2 = Appointment.Create(customerId, staffId, serviceId, slot2, 250000m, "نوبت دوم");

            db.Appointments.AddRange(apt1, apt2);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync($"{Endpoint}?customerId={customerId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<List<AppointmentDto>>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Message.Should().NotBeNullOrWhiteSpace();

        var appointments = envelope.Data;
        appointments.Should().NotBeNull();
        appointments.Should().HaveCount(2);

        // Verify ordering (date descending)
        appointments[0].Date.Should().Be(date1.ToString("yyyy-MM-dd"));
        appointments[0].CustomerName.Should().Be("مهدی کاظمی");
        appointments[0].StaffName.Should().Be("استاد حسینی");
        appointments[0].ServiceName.Should().Be("کوتاهی کلاسیک");
        appointments[0].Price.Should().Be(250000m);

        appointments[1].Date.Should().Be(date2.ToString("yyyy-MM-dd"));
    }

    [Fact]
    public async Task GetAppointments_ByCustomerSubRoute_Returns200WithAppointments()
    {
        // Arrange
        Guid customerId;
        Guid staffId;
        Guid serviceId;
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3));

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var user = User.Create("09121114477", DateTime.UtcNow, UserRole.Customer, "ساسان محمدی");
            var service = SalonService.Create("اصلاح ریش", "فرم‌دهی ریش", 20, 120000m, "Beard");
            var staff = StaffMember.Create("استاد رضایی", "rezaei-subroute-test", "09123332211", "", "Barber", 4, serviceIds: new List<Guid> { service.Id });

            db.Users.Add(user);
            db.SalonServices.Add(service);
            db.StaffMembers.Add(staff);
            await db.SaveChangesAsync();

            customerId = user.Id;
            staffId = staff.Id;
            serviceId = service.Id;

            var slot = TimeSlot.Create(date, new TimeOnly(15, 0), new TimeOnly(15, 20));
            var apt = Appointment.Create(customerId, staffId, serviceId, slot, 120000m);

            db.Appointments.Add(apt);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync($"{Endpoint}/customer/{customerId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<List<AppointmentDto>>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();

        var appointments = envelope.Data;
        appointments.Should().NotBeNull();
        appointments.Should().HaveCount(1);
        appointments[0].CustomerId.Should().Be(customerId);
        appointments[0].CustomerName.Should().Be("ساسان محمدی");
        appointments[0].StaffName.Should().Be("استاد رضایی");
        appointments[0].ServiceName.Should().Be("اصلاح ریش");
    }

    [Fact]
    public async Task GetAppointments_WithCustomerHavingNoAppointments_Returns200WithEmptyList()
    {
        // Arrange
        Guid customerId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var user = User.Create("09121119988", DateTime.UtcNow, UserRole.Customer, "مشتری جدید بدون نوبت");
            db.Users.Add(user);
            await db.SaveChangesAsync();

            customerId = user.Id;
        }

        // Act
        var response = await _client.GetAsync($"{Endpoint}?customerId={customerId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<List<AppointmentDto>>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAppointments_WithNonExistentCustomerId_Returns404NotFound()
    {
        // Arrange
        var missingCustomerId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"{Endpoint}?customerId={missingCustomerId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<object?>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GetAppointments_WithEmptyCustomerId_Returns400BadRequest()
    {
        // Arrange
        var emptyCustomerId = Guid.Empty;

        // Act
        var response = await _client.GetAsync($"{Endpoint}?customerId={emptyCustomerId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<object?>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeFalse();
    }

    private sealed record ApiResponseEnvelope<T>(T Data, bool Success, string Message);
}
