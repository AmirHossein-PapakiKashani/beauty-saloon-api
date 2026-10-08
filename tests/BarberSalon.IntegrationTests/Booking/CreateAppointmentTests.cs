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

public class CreateAppointmentTests : IClassFixture<BarberSalonWebFactory>
{
    private const string Endpoint = "/api/v1/appointments";

    private readonly HttpClient _client;
    private readonly BarberSalonWebFactory _factory;

    public CreateAppointmentTests(BarberSalonWebFactory factory)
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
    public async Task CreateAppointment_WithValidPayload_Returns201CreatedAndPersistsAppointment()
    {
        // Arrange
        Guid customerId;
        Guid staffId;
        Guid serviceId;
        var dateStr = GetFutureDateString(3);
        var timeStr = "11:00";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var user = User.Create("09121113355", DateTime.UtcNow, UserRole.Customer, "رضا مرادی");
            var service = SalonService.Create("اصلاح موی سر", "کوتاهی و اصلاح", 30, 200000m, "Haircut");
            var staff = StaffMember.Create(
                "استاد اکبری",
                "akbari-create-test",
                "09124445566",
                "آرایشگر",
                "Barber",
                6,
                serviceIds: new List<Guid> { service.Id });

            db.Users.Add(user);
            db.SalonServices.Add(service);
            db.StaffMembers.Add(staff);
            await db.SaveChangesAsync();

            customerId = user.Id;
            staffId = staff.Id;
            serviceId = service.Id;
        }

        var request = new CreateAppointmentRequest(
            customerId,
            staffId,
            serviceId,
            dateStr,
            timeStr,
            "نوبت ویژه");

        // Act
        var response = await _client.PostAsJsonAsync(Endpoint, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<CreateAppointmentResponse>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Message.Should().NotBeNullOrWhiteSpace();

        var result = envelope.Data;
        result.Should().NotBeNull();
        result.BookingCode.Should().StartWith("#");
        result.BookingCode.Length.Should().Be(7);

        var apt = result.Appointment;
        apt.Id.Should().NotBeEmpty();
        apt.CustomerId.Should().Be(customerId);
        apt.CustomerName.Should().Be("رضا مرادی");
        apt.StaffId.Should().Be(staffId);
        apt.StaffName.Should().Be("استاد اکبری");
        apt.ServiceId.Should().Be(serviceId);
        apt.ServiceName.Should().Be("اصلاح موی سر");
        apt.Date.Should().Be(dateStr);
        apt.Time.Should().Be(timeStr);
        apt.Status.Should().Be("pending");
        apt.Price.Should().Be(200000m);
        apt.Notes.Should().Be("نوبت ویژه");

        // Verify GET by ID returns the same appointment
        var getResponse = await _client.GetAsync($"{Endpoint}/{apt.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var getEnvelope = await getResponse.Content.ReadFromJsonAsync<ApiResponseEnvelope<AppointmentDto>>(JsonOptions);
        getEnvelope.Should().NotBeNull();
        getEnvelope!.Success.Should().BeTrue();
        getEnvelope.Data.Id.Should().Be(apt.Id);
    }

    [Fact]
    public async Task CreateAppointment_WithNonExistentCustomerId_Returns404NotFound()
    {
        // Arrange
        Guid staffId;
        Guid serviceId;
        var dateStr = GetFutureDateString(4);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var service = SalonService.Create("سرویس تست 404", "", 30, 100000m, "Hair");
            var staff = StaffMember.Create("پرسنل تست 404", "staff-test-404-1", "09120001111", "", "Barber", 3, serviceIds: new List<Guid> { service.Id });
            db.SalonServices.Add(service);
            db.StaffMembers.Add(staff);
            await db.SaveChangesAsync();

            staffId = staff.Id;
            serviceId = service.Id;
        }

        var request = new CreateAppointmentRequest(
            Guid.NewGuid(), // Non-existent
            staffId,
            serviceId,
            dateStr,
            "10:00");

        // Act
        var response = await _client.PostAsJsonAsync(Endpoint, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<object?>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAppointment_WithNonExistentStaffId_Returns404NotFound()
    {
        // Arrange
        Guid customerId;
        Guid serviceId;
        var dateStr = GetFutureDateString(4);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = User.Create("09120002222", DateTime.UtcNow, UserRole.Customer, "کاربر تست");
            var service = SalonService.Create("سرویس تست 404-2", "", 30, 100000m, "Hair");
            db.Users.Add(user);
            db.SalonServices.Add(service);
            await db.SaveChangesAsync();

            customerId = user.Id;
            serviceId = service.Id;
        }

        var request = new CreateAppointmentRequest(
            customerId,
            Guid.NewGuid(), // Non-existent
            serviceId,
            dateStr,
            "10:00");

        // Act
        var response = await _client.PostAsJsonAsync(Endpoint, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateAppointment_WithNonExistentServiceId_Returns404NotFound()
    {
        // Arrange
        Guid customerId;
        Guid staffId;
        var dateStr = GetFutureDateString(4);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = User.Create("09120003333", DateTime.UtcNow, UserRole.Customer, "کاربر تست");
            var staff = StaffMember.Create("پرسنل تست 404-3", "staff-test-404-3", "09120004444", "", "Barber", 3);
            db.Users.Add(user);
            db.StaffMembers.Add(staff);
            await db.SaveChangesAsync();

            customerId = user.Id;
            staffId = staff.Id;
        }

        var request = new CreateAppointmentRequest(
            customerId,
            staffId,
            Guid.NewGuid(), // Non-existent
            dateStr,
            "10:00");

        // Act
        var response = await _client.PostAsJsonAsync(Endpoint, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateAppointment_WithStaffNotPerformingService_Returns400BadRequest()
    {
        // Arrange
        Guid customerId;
        Guid staffId;
        Guid serviceId;
        var dateStr = GetFutureDateString(5);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = User.Create("09120005555", DateTime.UtcNow, UserRole.Customer, "کاربر تست");
            var service1 = SalonService.Create("سرویس 1", "", 30, 100000m, "Hair");
            var service2 = SalonService.Create("سرویس 2", "", 30, 150000m, "Skin");
            var staff = StaffMember.Create(
                "پرسنل تک‌سرویس",
                "staff-single-service",
                "09120006666",
                "",
                "Barber",
                3,
                serviceIds: new List<Guid> { service1.Id });

            db.Users.Add(user);
            db.SalonServices.AddRange(service1, service2);
            db.StaffMembers.Add(staff);
            await db.SaveChangesAsync();

            customerId = user.Id;
            staffId = staff.Id;
            serviceId = service2.Id; // Staff only performs service1
        }

        var request = new CreateAppointmentRequest(
            customerId,
            staffId,
            serviceId,
            dateStr,
            "10:00");

        // Act
        var response = await _client.PostAsJsonAsync(Endpoint, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<object?>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAppointment_WithConflictingSlot_Returns400BadRequest()
    {
        // Arrange
        Guid customerId1;
        Guid customerId2;
        Guid staffId;
        Guid serviceId;
        var dateStr = GetFutureDateString(6);
        var date = DateOnly.Parse(dateStr);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user1 = User.Create("09127770001", DateTime.UtcNow, UserRole.Customer, "کاربر اول");
            var user2 = User.Create("09127770002", DateTime.UtcNow, UserRole.Customer, "کاربر دوم");
            var service = SalonService.Create("سرویس رزرو تداخلی", "", 30, 120000m, "Hair");
            var staff = StaffMember.Create("پرسنل رزرو تداخلی", "staff-conflict-test", "09127770003", "", "Barber", 4, serviceIds: new List<Guid> { service.Id });

            db.Users.AddRange(user1, user2);
            db.SalonServices.Add(service);
            db.StaffMembers.Add(staff);
            await db.SaveChangesAsync();

            customerId1 = user1.Id;
            customerId2 = user2.Id;
            staffId = staff.Id;
            serviceId = service.Id;

            // Existing appointment on same slot
            var slot = TimeSlot.Create(date, new TimeOnly(14, 0), new TimeOnly(14, 30));
            var existingApt = Appointment.Create(customerId1, staffId, serviceId, slot, 120000m);
            db.Appointments.Add(existingApt);
            await db.SaveChangesAsync();
        }

        var request = new CreateAppointmentRequest(
            customerId2,
            staffId,
            serviceId,
            dateStr,
            "14:00");

        // Act
        var response = await _client.PostAsJsonAsync(Endpoint, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<object?>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAppointment_WithPastDate_Returns400BadRequest()
    {
        // Arrange
        var request = new CreateAppointmentRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "2020-01-01",
            "10:00");

        // Act
        var response = await _client.PostAsJsonAsync(Endpoint, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateAppointment_WithInvalidTimeFormat_Returns400BadRequest()
    {
        // Arrange
        var request = new CreateAppointmentRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            GetFutureDateString(7),
            "invalid-time");

        // Act
        var response = await _client.PostAsJsonAsync(Endpoint, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetById_WithNonExistentId_Returns404NotFound()
    {
        // Act
        var response = await _client.GetAsync($"{Endpoint}/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<object?>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAppointment_WithStaffCustomServicePrice_PersistsCustomStaffPrice()
    {
        // Arrange
        Guid customerId;
        Guid staffId;
        Guid serviceId;
        var dateStr = GetFutureDateString(5);
        var timeStr = "14:00";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var user = User.Create($"0912{Random.Shared.Next(1000000, 9999999)}", DateTime.UtcNow, UserRole.Customer, "مشتری قیمت سفارشی");
            var service = SalonService.Create("رنگ و لایت", "رنگ تخصصی مو", 60, 300000m, "Color");
            var staff = StaffMember.Create(
                "استاد برتر",
                $"master-{Guid.NewGuid():N}",
                $"0912{Random.Shared.Next(1000000, 9999999)}",
                "آرایشگر ارشد",
                "Senior Barber",
                7,
                serviceIds: new List<Guid> { service.Id },
                servicePrices: new Dictionary<Guid, decimal> { [service.Id] = 450000m });

            db.Users.Add(user);
            db.SalonServices.Add(service);
            db.StaffMembers.Add(staff);
            await db.SaveChangesAsync();

            customerId = user.Id;
            staffId = staff.Id;
            serviceId = service.Id;
        }

        var request = new CreateAppointmentRequest(
            customerId,
            staffId,
            serviceId,
            dateStr,
            timeStr,
            "سفارش با دستمزد سفارشی آرایشگر");

        // Act
        var response = await _client.PostAsJsonAsync(Endpoint, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<CreateAppointmentResponse>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();

        var apt = envelope.Data.Appointment;
        apt.Price.Should().Be(450000m); // Custom staff rate should be used instead of service base price (300000m)

        // Verify in database via GET by ID
        var getResponse = await _client.GetAsync($"{Endpoint}/{apt.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var getEnvelope = await getResponse.Content.ReadFromJsonAsync<ApiResponseEnvelope<AppointmentDto>>(JsonOptions);
        getEnvelope.Should().NotBeNull();
        getEnvelope!.Data.Price.Should().Be(450000m);
    }

    private sealed record ApiResponseEnvelope<T>(T Data, bool Success, string Message);
}
