using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BarberSalon.Application.Waitlist.DTOs;
using BarberSalon.Domain.Customers.Entities;
using BarberSalon.Domain.SalonServices.Entities;
using BarberSalon.Domain.Staff.Entities;
using BarberSalon.Domain.Waitlist.Entities;
using BarberSalon.Infrastructure.Persistence;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BarberSalon.IntegrationTests.Waitlist;

public class WaitlistClaimIntegrationTests : IClassFixture<BarberSalonWebFactory>
{
    private readonly HttpClient _client;
    private readonly BarberSalonWebFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public WaitlistClaimIntegrationTests(BarberSalonWebFactory factory)
    {
        _factory = factory;
        _client = factory.GetClient();
    }

    [Fact]
    public async Task ClaimWaitlistSlot_WhenNotified_UpdatesStatusToClaimed()
    {
        // Arrange
        Guid entryId;
        Guid customerId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var customer = Customer.Create("مشتری صف", "09121112233");
            db.Customers.Add(customer);
            customerId = customer.Id;

            var staff = StaffMember.Create("آرایشگر", "barber-slug", "09123334455", "", "Barber", 3);
            db.StaffMembers.Add(staff);

            var service = SalonService.Create("کوتاهی", "توضیح", 30, 200000, "haircut");
            db.SalonServices.Add(service);

            var entry = WaitlistEntry.Create(
                customerId,
                customer.FullName,
                customer.PhoneNumber,
                staff.Id,
                staff.FullName,
                service.Id,
                service.Name,
                "1403/10/20",
                "10:00"
            );
            entry.Notify();
            db.WaitlistEntries.Add(entry);
            await db.SaveChangesAsync();
            entryId = entry.Id;
        }

        // Act: Call Claim endpoint
        var claimResponse = await _client.PostAsync($"/api/v1/waitlist/{entryId}/claim", null);

        // Assert
        claimResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await claimResponse.Content.ReadFromJsonAsync<ApiResponseEnvelope<WaitlistEntryDto>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
        envelope.Data!.Status.Should().Be("claimed");

        // Act: Get customer notifications
        var notifResponse = await _client.GetAsync($"/api/v1/waitlist/customer/{customerId}/notifications");
        notifResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed record ApiResponseEnvelope<T>(T? Data, bool Success, string Message);
}
