using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BarberSalon.Application.Booking.DTOs;
using BarberSalon.Domain.Customers.Entities;
using BarberSalon.Domain.Booking.Entities;
using BarberSalon.Domain.Booking.ValueObjects;
using BarberSalon.Domain.SalonServices.Entities;
using BarberSalon.Domain.Staff.Entities;
using BarberSalon.Infrastructure.Persistence;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BarberSalon.IntegrationTests.Appointments;

public sealed class UpdateAppointmentIntegrationTests : IClassFixture<BarberSalonWebFactory>
{
    private readonly BarberSalonWebFactory _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public UpdateAppointmentIntegrationTests(BarberSalonWebFactory factory)
    {
        _factory = factory;
        _client = factory.GetClient();
    }

    [Fact]
    public async Task Put_UpdateAppointment_UpdatesDetailsAndReturnsOk()
    {
        // Arrange
        Guid appointmentId;
        Guid staffId1;
        Guid staffId2;
        Guid serviceId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var customer = Customer.Create("Test Customer", "09123334455");
            db.Customers.Add(customer);

            var staff1 = StaffMember.Create("Old Staff", "old-staff", "09121111111", "", "Stylist", 3);
            var staff2 = StaffMember.Create("New Staff", "new-staff", "09122222222", "", "Senior Stylist", 5);
            db.StaffMembers.AddRange(staff1, staff2);

            var service = SalonService.Create("Haircut", "haircut description", 45, 500000m, "haircut");
            db.SalonServices.Add(service);

            var timeSlot = TimeSlot.Create(DateOnly.FromDateTime(DateTime.Today.AddDays(2)), new TimeOnly(10, 0), new TimeOnly(10, 45));
            var appointment = Appointment.Create(customer.Id, staff1.Id, service.Id, timeSlot, 500000m, "Old notes");
            db.Appointments.Add(appointment);

            await db.SaveChangesAsync();

            appointmentId = appointment.Id;
            staffId1 = staff1.Id;
            staffId2 = staff2.Id;
            serviceId = service.Id;
        }

        var newDate = DateOnly.FromDateTime(DateTime.Today.AddDays(3)).ToString("yyyy-MM-dd");
        var updateRequest = new UpdateAppointmentRequest(
            staffId2,
            serviceId,
            newDate,
            "14:00",
            550000m,
            "Updated special request");

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/appointments/{appointmentId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        var data = content.GetProperty("data");
        data.GetProperty("staffId").GetString().Should().Be(staffId2.ToString());
        data.GetProperty("date").GetString().Should().Be(newDate);
        data.GetProperty("time").GetString().Should().Be("14:00");
        data.GetProperty("notes").GetString().Should().Be("Updated special request");
    }
}
