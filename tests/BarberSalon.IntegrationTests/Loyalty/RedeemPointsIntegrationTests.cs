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

public class RedeemPointsIntegrationTests : IClassFixture<BarberSalonWebFactory>
{
    private readonly HttpClient _client;
    private readonly BarberSalonWebFactory _factory;

    public RedeemPointsIntegrationTests(BarberSalonWebFactory factory)
    {
        _factory = factory;
        _client = factory.GetClient();
    }

    [Fact]
    public async Task RedeemPoints_WithSufficientBalance_Returns200AndDeductsPoints()
    {
        // Arrange
        var customer = Customer.Create("مشتری وفادار", "09123456789");
        var account = LoyaltyAccount.Create(customer.Id);
        account.AddPoints(500);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Customers.Add(customer);
            db.LoyaltyAccounts.Add(account);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/loyalty/{customer.Id}/redeem",
            new RedeemPointsRequest(100)
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var updatedAccount = await db.LoyaltyAccounts.FindAsync(account.Id);
            updatedAccount.Should().NotBeNull();
            updatedAccount!.PointsBalance.Should().Be(400);
        }
    }

    [Fact]
    public async Task RedeemPoints_WithInsufficientBalance_Returns400()
    {
        // Arrange
        var customer = Customer.Create("مشتری کم‌امتیاز", "09129998877");
        var account = LoyaltyAccount.Create(customer.Id);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Customers.Add(customer);
            db.LoyaltyAccounts.Add(account);
            await db.SaveChangesAsync();
        }

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/loyalty/{customer.Id}/redeem",
            new RedeemPointsRequest(9999)
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
