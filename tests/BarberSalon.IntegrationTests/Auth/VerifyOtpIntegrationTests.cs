using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BarberSalon.Application.Auth.DTOs;
using BarberSalon.Domain.Auth.Entities;
using BarberSalon.Domain.Auth.Enums;
using BarberSalon.Infrastructure.Persistence;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BarberSalon.IntegrationTests.Auth;

public class VerifyOtpIntegrationTests : IClassFixture<BarberSalonWebFactory>
{
    private const string Endpoint = "/api/v1/auth/verify-otp";
    private readonly HttpClient _client;
    private readonly BarberSalonWebFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public VerifyOtpIntegrationTests(BarberSalonWebFactory factory)
    {
        _factory = factory;
        _client = factory.GetClient();
    }

    [Fact]
    public async Task VerifyOtp_WithValidCodeAndNewUser_Returns200_CreatesUserInDb_AndMarksOtpUsed()
    {
        // Arrange
        var phone = "09121112233";
        var code = "54321";
        var now = DateTime.UtcNow;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var otp = OtpCode.Create(phone, code, now);
            await db.OtpCodes.AddAsync(otp);
            await db.SaveChangesAsync();
        }

        var request = new VerifyOtpRequest(phone, code);

        // Act
        var response = await _client.PostAsJsonAsync(Endpoint, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<VerifyOtpResponse>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
        envelope.Data!.Success.Should().BeTrue();
        envelope.Data.IsNewUser.Should().BeTrue();
        envelope.Data.User.Should().NotBeNull();
        envelope.Data.User!.PhoneNumber.Should().Be(phone);
        envelope.Data.User.Role.Should().Be("Customer");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var dbOtp = await db.OtpCodes.FirstOrDefaultAsync(o => o.PhoneNumber == phone && o.Code == code);
            dbOtp.Should().NotBeNull();
            dbOtp!.IsUsed.Should().BeTrue();

            var dbUser = await db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phone);
            dbUser.Should().NotBeNull();
            dbUser!.PhoneNumber.Should().Be(phone);
            dbUser.Role.Should().Be(UserRole.Customer);
            dbUser.IsActive.Should().BeTrue();
        }
    }

    [Fact]
    public async Task VerifyOtp_WithValidCodeAndExistingUser_Returns200_ReturnsExistingUser_AndIsNewUserFalse()
    {
        // Arrange
        var phone = "09124445566";
        var code = "12345";
        var now = DateTime.UtcNow;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = User.Create(phone, now.AddDays(-5), UserRole.Admin, "مدیر سالن");
            var otp = OtpCode.Create(phone, code, now);
            await db.Users.AddAsync(user);
            await db.OtpCodes.AddAsync(otp);
            await db.SaveChangesAsync();
        }

        var request = new VerifyOtpRequest(phone, code);

        // Act
        var response = await _client.PostAsJsonAsync(Endpoint, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<VerifyOtpResponse>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
        envelope.Data!.Success.Should().BeTrue();
        envelope.Data.IsNewUser.Should().BeFalse();
        envelope.Data.User.Should().NotBeNull();
        envelope.Data.User!.PhoneNumber.Should().Be(phone);
        envelope.Data.User.Name.Should().Be("مدیر سالن");
        envelope.Data.User.Role.Should().Be("Admin");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var userCount = await db.Users.CountAsync(u => u.PhoneNumber == phone);
            userCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task VerifyOtp_WithIncorrectCode_Returns400BadRequest_AndDoesNotMarkOtpUsed()
    {
        // Arrange
        var phone = "09127778899";
        var code = "65432";
        var now = DateTime.UtcNow;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var otp = OtpCode.Create(phone, code, now);
            await db.OtpCodes.AddAsync(otp);
            await db.SaveChangesAsync();
        }

        var request = new VerifyOtpRequest(phone, "99999");

        // Act
        var response = await _client.PostAsJsonAsync(Endpoint, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<object?>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeFalse();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var dbOtp = await db.OtpCodes.FirstOrDefaultAsync(o => o.PhoneNumber == phone && o.Code == code);
            dbOtp.Should().NotBeNull();
            dbOtp!.IsUsed.Should().BeFalse();

            var dbUser = await db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phone);
            dbUser.Should().BeNull();
        }
    }

    [Fact]
    public async Task VerifyOtp_WithExpiredCode_Returns400BadRequest()
    {
        // Arrange
        var phone = "09123334455";
        var code = "11223";
        var past = DateTime.UtcNow.AddMinutes(-10);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var otp = OtpCode.Create(phone, code, past);
            await db.OtpCodes.AddAsync(otp);
            await db.SaveChangesAsync();
        }

        var request = new VerifyOtpRequest(phone, code);

        // Act
        var response = await _client.PostAsJsonAsync(Endpoint, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<object?>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyOtp_WithEmptyPhoneNumber_Returns400BadRequest()
    {
        // Arrange
        var request = new VerifyOtpRequest("", "12345");

        // Act
        var response = await _client.PostAsJsonAsync(Endpoint, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<object?>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyOtp_WithInvalidCodeFormat_Returns400BadRequest()
    {
        // Arrange
        var request = new VerifyOtpRequest("09123456789", "123");

        // Act
        var response = await _client.PostAsJsonAsync(Endpoint, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<object?>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeFalse();
    }

    private sealed record ApiResponseEnvelope<T>(T? Data, bool Success, string Message);
}
