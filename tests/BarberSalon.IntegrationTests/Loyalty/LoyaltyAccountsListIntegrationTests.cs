using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BarberSalon.Application.Loyalty.DTOs;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Xunit;

namespace BarberSalon.IntegrationTests.Loyalty;

public class LoyaltyAccountsListIntegrationTests : IClassFixture<BarberSalonWebFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public LoyaltyAccountsListIntegrationTests(BarberSalonWebFactory factory)
    {
        _client = factory.GetClient();
    }

    [Fact]
    public async Task GetAllAccounts_Returns200WithList()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/loyalty/accounts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<List<LoyaltyAccountDto>>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task RedeemPoints_WhenRedeeming_ReturnsUpdatedBalanceOrError()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        // 1. Get or create account
        var accRes = await _client.GetAsync($"/api/v1/loyalty/{customerId}");
        accRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var accEnvelope = await accRes.Content.ReadFromJsonAsync<ApiResponseEnvelope<LoyaltyAccountDto>>(JsonOptions);
        var referralCode = accEnvelope!.Data.ReferralCode;

        // 2. Give points via referral application
        var friendId = Guid.NewGuid();
        await _client.PostAsJsonAsync("/api/v1/loyalty/apply-referral", new ApplyReferralRequest(referralCode, friendId));

        // 3. Redeem points
        var redeemReq = new RedeemPointsRequest(20);
        var redeemRes = await _client.PostAsJsonAsync($"/api/v1/loyalty/{customerId}/redeem", redeemReq);

        // Assert
        redeemRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var redeemEnvelope = await redeemRes.Content.ReadFromJsonAsync<ApiResponseEnvelope<LoyaltyAccountDto>>(JsonOptions);
        redeemEnvelope.Should().NotBeNull();
        redeemEnvelope!.Success.Should().BeTrue();
        redeemEnvelope.Data.PointsBalance.Should().Be(30); // 50 - 20 = 30
    }

    private sealed record ApiResponseEnvelope<T>(T Data, bool Success, string Message);
}
