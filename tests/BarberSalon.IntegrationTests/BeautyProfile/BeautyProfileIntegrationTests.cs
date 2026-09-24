using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BarberSalon.Application.BeautyProfile.DTOs;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Xunit;

namespace BarberSalon.IntegrationTests.BeautyProfile;

public class BeautyProfileIntegrationTests : IClassFixture<BarberSalonWebFactory>
{
    private readonly HttpClient _client;
    private readonly BarberSalonWebFactory _factory;

    public BeautyProfileIntegrationTests(BarberSalonWebFactory factory)
    {
        _factory = factory;
        _client = factory.GetClient();
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task GetBeautyProfile_Returns200WithProfile()
    {
        var customerId = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/v1/beauty-profile/{customerId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<BeautyProfileDto>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.CustomerId.Should().Be(customerId);
    }

    [Fact]
    public async Task UpdateBeautyProfile_UpsertsAndReturns200()
    {
        var customerId = Guid.NewGuid();
        var request = new UpdateBeautyProfileRequest(
            "normal",
            "طبیعی مشکی",
            "حساسیت به دکلره",
            "کوتاهی کلاسیک",
            "مشتری وفادار",
            "چرب",
            "آلرژی فصلی"
        );

        var putResponse = await _client.PutAsJsonAsync($"/api/v1/beauty-profile/{customerId}", request);
        putResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await putResponse.Content.ReadFromJsonAsync<ApiResponseEnvelope<BeautyProfileDto>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.CustomerId.Should().Be(customerId);
        envelope.Data.HairType.Should().Be("normal");
        envelope.Data.CurrentHairColor.Should().Be("طبیعی مشکی");
        envelope.Data.Sensitivities.Should().Be("حساسیت به دکلره");
        envelope.Data.Preferences.Should().Be("کوتاهی کلاسیک");
        envelope.Data.Notes.Should().Be("مشتری وفادار");
        envelope.Data.SkinType.Should().Be("چرب");
        envelope.Data.Allergies.Should().Be("آلرژی فصلی");

        // Verify GET returns updated data
        var getResponse = await _client.GetAsync($"/api/v1/beauty-profile/{customerId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var getEnvelope = await getResponse.Content.ReadFromJsonAsync<ApiResponseEnvelope<BeautyProfileDto>>(JsonOptions);
        getEnvelope!.Data.HairType.Should().Be("normal");
    }

    private sealed record ApiResponseEnvelope<T>(T Data, bool Success, string Message);
}
