using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BarberSalon.Application.Booking.DTOs;
using BarberSalon.Domain.Booking.Entities;
using BarberSalon.Domain.Booking.ValueObjects;
using BarberSalon.Domain.Staff.Entities;
using BarberSalon.Infrastructure.Persistence;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BarberSalon.IntegrationTests.Booking;

public class GetAvailableTimeSlotsTests : IClassFixture<BarberSalonWebFactory>
{
    private const string Endpoint = "/api/v1/time-slots";

    private readonly HttpClient _client;
    private readonly BarberSalonWebFactory _factory;

    public GetAvailableTimeSlotsTests(BarberSalonWebFactory factory)
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
    public async Task GetAvailableTimeSlots_WithValidDate_Returns200WithStandardEnvelope()
    {
        // Arrange
        var testDate = GetFutureDateString(5);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var staff = StaffMember.Create("آقای کریمی", "karimi-slot-test1", "09121112233", "آرایشگر", "آرایشگر", 5);
            db.StaffMembers.Add(staff);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync($"{Endpoint}?date={testDate}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<List<TimeSlotDto>>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Message.Should().NotBeNullOrWhiteSpace();
        envelope.Data.Should().HaveCount(19);
        envelope.Data.First().Time.Should().Be("09:00");
        envelope.Data.Last().Time.Should().Be("18:00");
        envelope.Data.Should().OnlyContain(s => s.Available);
    }

    [Fact]
    public async Task GetAvailableTimeSlots_WithStaffId_WhenStaffHasAppointment_MarksSlotUnavailable()
    {
        // Arrange
        var testDateStr = GetFutureDateString(6);
        var testDate = DateOnly.Parse(testDateStr);
        Guid staffId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var staff = StaffMember.Create("خانم احمدی", "ahmadi-slot-test1", "09129876543", "متخصص رنگ", "متخصص رنگ", 8);
            db.StaffMembers.Add(staff);
            await db.SaveChangesAsync();
            staffId = staff.Id;

            var slot = TimeSlot.Create(testDate, new TimeOnly(11, 30), new TimeOnly(12, 0));
            var apt = Appointment.Create(Guid.NewGuid(), staffId, Guid.NewGuid(), slot, 350000m);
            apt.Confirm();

            db.Appointments.Add(apt);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync($"{Endpoint}?date={testDateStr}&staffId={staffId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<List<TimeSlotDto>>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().HaveCount(19);

        var slot1130 = envelope.Data.Single(s => s.Time == "11:30");
        slot1130.Available.Should().BeFalse();

        var otherSlots = envelope.Data.Where(s => s.Time != "11:30");
        otherSlots.Should().OnlyContain(s => s.Available);
    }

    [Fact]
    public async Task GetAvailableTimeSlots_WithNonExistentStaffId_Returns404NotFound()
    {
        // Arrange
        var testDate = GetFutureDateString(7);
        var missingStaffId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"{Endpoint}?date={testDate}&staffId={missingStaffId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<object?>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GetAvailableTimeSlots_WithInvalidDateFormat_Returns400BadRequest()
    {
        // Act
        var response = await _client.GetAsync($"{Endpoint}?date=invalid-date");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<object?>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GetAvailableTimeSlots_WithoutDate_Returns400BadRequest()
    {
        // Act
        var response = await _client.GetAsync(Endpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<object?>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GetAvailableTimeSlots_WithPastDate_Returns400BadRequest()
    {
        // Act
        var response = await _client.GetAsync($"{Endpoint}?date=2020-01-01");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<object?>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeFalse();
    }

    private sealed record ApiResponseEnvelope<T>(T Data, bool Success, string Message);
}
