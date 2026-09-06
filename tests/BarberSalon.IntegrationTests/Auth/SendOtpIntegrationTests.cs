using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BarberSalon.Application.Auth.DTOs;
using BarberSalon.Infrastructure.Persistence;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BarberSalon.IntegrationTests.Auth;

public class SendOtpIntegrationTests : IClassFixture<BarberSalonWebFactory>
{
    private const string Endpoint = "/api/v1/auth/send-otp";
    private readonly HttpClient _client;
    private readonly BarberSalonWebFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public SendOtpIntegrationTests(BarberSalonWebFactory factory)
    {
        _factory = factory;
        _client = factory.GetClient();
    }

    [Fact]
    public async Task SendOtp_WithValidPhoneNumber_Returns200AndSavesOtpToDatabase()
    {
        // Arrange
        var request = new SendOtpRequest("09123456789");

        // Act
        var response = await _client.PostAsJsonAsync(Endpoint, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<SendOtpResponse>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
        envelope.Data!.Sent.Should().BeTrue();
        envelope.Data.ExpiresInSeconds.Should().Be(300);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var savedOtp = await db.OtpCodes
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(o => o.PhoneNumber == "09123456789");

        savedOtp.Should().NotBeNull();
        savedOtp!.Code.Should().HaveLength(5);
        savedOtp.IsUsed.Should().BeFalse();
    }

    [Fact]
    public async Task SendOtp_WithEmptyPhoneNumber_Returns400BadRequest()
    {
        // Arrange
        var request = new SendOtpRequest("");

        // Act
        var response = await _client.PostAsJsonAsync(Endpoint, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<object?>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task SendOtp_WithInvalidPhoneNumberFormat_Returns400BadRequest()
    {
        // Arrange
        var request = new SendOtpRequest("1234");

        // Act
        var response = await _client.PostAsJsonAsync(Endpoint, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<object?>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task SendOtp_WhenCalledMultipleTimes_InvalidatesPreviousCodesInDatabase()
    {
        // Arrange
        var phone = "09129998877";
        var request = new SendOtpRequest(phone);

        // Act - First OTP
        var response1 = await _client.PostAsJsonAsync(Endpoint, request);
        response1.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act - Second OTP
        var response2 = await _client.PostAsJsonAsync(Endpoint, request);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var otps = await db.OtpCodes
            .Where(o => o.PhoneNumber == phone)
            .OrderBy(o => o.CreatedAt)
            .ToListAsync();

        otps.Should().HaveCount(2);
        otps[0].IsUsed.Should().BeTrue();
        otps[1].IsUsed.Should().BeFalse();
    }

    private sealed record ApiResponseEnvelope<T>(T? Data, bool Success, string Message);
}
