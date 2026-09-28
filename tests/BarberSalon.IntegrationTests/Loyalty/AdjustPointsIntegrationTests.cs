using System.Net;
using System.Net.Http.Json;
using BarberSalon.Application.Loyalty.DTOs;
using BarberSalon.Domain.Customers.Entities;
using BarberSalon.Domain.Loyalty.Entities;
using BarberSalon.Infrastructure.Persistence;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BarberSalon.IntegrationTests.Loyalty;

public class AdjustPointsIntegrationTests : IClassFixture<BarberSalonWebFactory>
{
    private readonly HttpClient _client;
    private readonly BarberSalonWebFactory _factory;

    public AdjustPointsIntegrationTests(BarberSalonWebFactory factory)
    {
        _factory = factory;
        _client = factory.GetClient();
    }

    [Fact]
    public async Task AdjustPoints_AddPoints_Returns200AndIncreasesBalance()
    {
        // Arrange
        var customer = Customer.Create("مشتری تنظیم امتیاز", "09127776655");
        var account = LoyaltyAccount.Create(customer.Id);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Customers.Add(customer);
            db.LoyaltyAccounts.Add(account);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/loyalty/accounts/{customer.Id}/adjust-points",
            new AdjustPointsRequest(200, "جایزه تست")
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var updated = await db.LoyaltyAccounts.FindAsync(account.Id);
            updated.Should().NotBeNull();
            updated!.PointsBalance.Should().Be(200);
        }
    }

    [Fact]
    public async Task AdjustPoints_DeductPoints_Returns200()
    {
        // Arrange
        var customer = Customer.Create("مشتری کسر", "09124443322");
        var account = LoyaltyAccount.Create(customer.Id);
        account.AddPoints(300);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Customers.Add(customer);
            db.LoyaltyAccounts.Add(account);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/loyalty/accounts/{customer.Id}/adjust-points",
            new AdjustPointsRequest(-100, "کسر تست")
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var updated = await db.LoyaltyAccounts.FindAsync(account.Id);
            updated.Should().NotBeNull();
            updated!.PointsBalance.Should().Be(200);
        }
    }
}
