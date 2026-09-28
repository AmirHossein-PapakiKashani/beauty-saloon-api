using System.Net;
using BarberSalon.Domain.BeautyProfile.Entities;
using BarberSalon.Domain.Customers.Entities;
using BarberSalon.Infrastructure.Persistence;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BarberSalon.IntegrationTests.BeautyProfile;

public class DeleteHistoryEntryIntegrationTests : IClassFixture<BarberSalonWebFactory>
{
    private readonly HttpClient _client;
    private readonly BarberSalonWebFactory _factory;

    public DeleteHistoryEntryIntegrationTests(BarberSalonWebFactory factory)
    {
        _factory = factory;
        _client = factory.GetClient();
    }

    [Fact]
    public async Task DeleteHistoryEntry_WithExistingEntry_Returns200()
    {
        // Arrange
        var customer = Customer.Create("مشتری تاریخچه", "09121112233");
        var entry = BeautyHistoryEntry.Create(customer.Id, "کوتاهی", "رضا", "1403/10/15", "فرمول", "یادداشت تست");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Customers.Add(customer);
            db.BeautyHistoryEntries.Add(entry);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.DeleteAsync($"/api/v1/beauty-profile/history/{entry.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var deleted = await db.BeautyHistoryEntries.FindAsync(entry.Id);
            deleted.Should().BeNull();
        }
    }

    [Fact]
    public async Task DeleteHistoryEntry_WithNonExistentId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/v1/beauty-profile/history/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
