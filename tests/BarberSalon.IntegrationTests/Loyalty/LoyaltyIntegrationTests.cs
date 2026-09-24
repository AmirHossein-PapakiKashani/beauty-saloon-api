using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BarberSalon.Application.Loyalty.DTOs;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Xunit;

namespace BarberSalon.IntegrationTests.Loyalty;

public class LoyaltyIntegrationTests : IClassFixture<BarberSalonWebFactory>
{
    private readonly HttpClient _client;
    private readonly BarberSalonWebFactory _factory;

    public LoyaltyIntegrationTests(BarberSalonWebFactory factory)
    {
        _factory = factory;
        _client = factory.GetClient();
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task GetLoyaltyAccount_WhenCustomerExists_Returns200AndValidAccount()
    {
        var customerId = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/v1/loyalty/{customerId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<LoyaltyAccountDto>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.CustomerId.Should().Be(customerId);
        envelope.Data.Tier.Should().Be("bronze");
        envelope.Data.ReferralCode.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetReferrals_Returns200WithList()
    {
        var response = await _client.GetAsync("/api/v1/loyalty/referrals");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<List<ReferralDto>>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task ValidateAndApplyReferralCode_ExecutesSuccessfully()
    {
        var customerId = Guid.NewGuid();
        // 1. Get account to obtain referral code
        var accRes = await _client.GetAsync($"/api/v1/loyalty/accounts/{customerId}");
        accRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var acc = (await accRes.Content.ReadFromJsonAsync<ApiResponseEnvelope<LoyaltyAccountDto>>(JsonOptions))!.Data;

        // 2. Validate referral code
        var valRes = await _client.GetAsync($"/api/v1/loyalty/validate-referral?code={acc.ReferralCode}");
        valRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. Apply referral code
        var friendCustomerId = Guid.NewGuid();
        var applyRes = await _client.PostAsJsonAsync("/api/v1/loyalty/apply-referral", new ApplyReferralRequest(acc.ReferralCode, friendCustomerId));
        applyRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed record ApiResponseEnvelope<T>(T Data, bool Success, string Message);
}
