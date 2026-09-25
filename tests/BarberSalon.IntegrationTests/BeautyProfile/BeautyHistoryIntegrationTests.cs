using System.Net;
using System.Net.Http.Json;
using BarberSalon.API.Common;
using BarberSalon.Application.BeautyProfile.DTOs;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Xunit;

namespace BarberSalon.IntegrationTests.BeautyProfile;

public class BeautyHistoryIntegrationTests : IClassFixture<BarberSalonWebFactory>
{
    private readonly HttpClient _client;
    public BeautyHistoryIntegrationTests(BarberSalonWebFactory factory) => _client = factory.GetClient();

    [Fact]
    public async Task AddAndGetBeautyHistory_ReturnsCreatedEntry()
    {
        var customerId = Guid.NewGuid();
        var req = new CreateBeautyHistoryEntryRequest(
            "کوتاهی و لایت", "علی کریمی", "۱۴۰۳/۱۰/۱۵", "فرمول لایت با دکلره ۸", "بدون حساسیت", null);

        var postRes = await _client.PostAsJsonAsync($"/api/v1/beauty-profile/{customerId}/history", req);
        postRes.StatusCode.Should().Be(HttpStatusCode.Created);

        var getRes = await _client.GetAsync($"/api/v1/beauty-profile/{customerId}/history");
        getRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await getRes.Content.ReadFromJsonAsync<ApiResponse<List<BeautyHistoryEntryDto>>>();
        envelope!.Data.Should().ContainSingle(x => x.ServiceName == "کوتاهی و لایت");
    }
}
