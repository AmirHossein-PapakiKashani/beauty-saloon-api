using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BarberSalon.Application.Loyalty.DTOs;
using BarberSalon.Domain.Customers.Entities;
using BarberSalon.Domain.Loyalty.Entities;
using BarberSalon.Infrastructure.Persistence;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BarberSalon.IntegrationTests.Loyalty;

public class LoyaltyTransactionsIntegrationTests : IClassFixture<BarberSalonWebFactory>
{
    private readonly HttpClient _client;
    private readonly BarberSalonWebFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public LoyaltyTransactionsIntegrationTests(BarberSalonWebFactory factory)
    {
        _factory = factory;
        _client = factory.GetClient();
    }

    [Fact]
    public async Task GetTransactions_WhenPointsRedeemed_ReturnsTransactionRecord()
    {
        // Arrange
        Guid customerId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var customer = Customer.Create("مشتری وفادار", "09129998877");
            db.Customers.Add(customer);
            customerId = customer.Id;

            var account = LoyaltyAccount.Create(customerId);
            account.AddPoints(500);
            db.LoyaltyAccounts.Add(account);
            await db.SaveChangesAsync();
        }

        // Act: Redeem points
        var redeemPayload = new RedeemPointsRequest(200);
        var redeemResponse = await _client.PostAsJsonAsync($"/api/v1/loyalty/{customerId}/redeem", redeemPayload);
        redeemResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act: Get transactions
        var txResponse = await _client.GetAsync($"/api/v1/loyalty/accounts/{customerId}/transactions");

        // Assert
        txResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await txResponse.Content.ReadFromJsonAsync<ApiResponseEnvelope<List<LoyaltyTransactionDto>>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
        envelope.Data.Should().ContainSingle();
        envelope.Data![0].Points.Should().Be(-200);
        envelope.Data[0].Type.Should().Be("redeemed");
    }

    private sealed record ApiResponseEnvelope<T>(T? Data, bool Success, string Message);
}
