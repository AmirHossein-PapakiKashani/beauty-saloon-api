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

public class UpdateUserProfileIntegrationTests : IClassFixture<BarberSalonWebFactory>
{
    private readonly HttpClient _client;
    private readonly BarberSalonWebFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public UpdateUserProfileIntegrationTests(BarberSalonWebFactory factory)
    {
        _factory = factory;
        _client = factory.GetClient();
    }

    [Fact]
    public async Task UpdateProfile_WithValidName_Returns200_UpdatesUserInDatabase()
    {
        // Arrange
        var phone = "09129998877";
        var user = User.Create(phone, DateTime.UtcNow, UserRole.Customer, "نام اولیه");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Users.AddAsync(user);
            await db.SaveChangesAsync();
        }

        var request = new UpdateUserProfileRequest("زهرا عباسی");

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/users/{user.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<UserDto>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
        envelope.Data!.Id.Should().Be(user.Id);
        envelope.Data.PhoneNumber.Should().Be(phone);
        envelope.Data.Name.Should().Be("زهرا عباسی");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var dbUser = await db.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
            dbUser.Should().NotBeNull();
            dbUser!.FullName.Should().Be("زهرا عباسی");
        }
    }

    [Fact]
    public async Task UpdateProfile_ViaSubroute_Returns200_UpdatesUserInDatabase()
    {
        // Arrange
        var phone = "09128887766";
        var user = User.Create(phone, DateTime.UtcNow, UserRole.Customer);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Users.AddAsync(user);
            await db.SaveChangesAsync();
        }

        var request = new UpdateUserProfileRequest("مریم احمدی");

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/users/{user.Id}/profile", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<UserDto>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data!.Name.Should().Be("مریم احمدی");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var dbUser = await db.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
            dbUser.Should().NotBeNull();
            dbUser!.FullName.Should().Be("مریم احمدی");
        }
    }

    [Fact]
    public async Task UpdateProfile_ViaPatch_Returns200_UpdatesUserInDatabase()
    {
        // Arrange
        var phone = "09127776655";
        var user = User.Create(phone, DateTime.UtcNow, UserRole.Customer);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Users.AddAsync(user);
            await db.SaveChangesAsync();
        }

        var request = new UpdateUserProfileRequest("الهام حسینی");

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/v1/users/{user.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<UserDto>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data!.Name.Should().Be("الهام حسینی");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var dbUser = await db.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
            dbUser.Should().NotBeNull();
            dbUser!.FullName.Should().Be("الهام حسینی");
        }
    }

    [Fact]
    public async Task UpdateProfile_WhenUserDoesNotExist_Returns404NotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var request = new UpdateUserProfileRequest("کاربر ناشناس");

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/users/{nonExistentId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<object?>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateProfile_WithExcessiveNameLength_Returns400BadRequest()
    {
        // Arrange
        var phone = "09126665544";
        var user = User.Create(phone, DateTime.UtcNow, UserRole.Customer);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Users.AddAsync(user);
            await db.SaveChangesAsync();
        }

        var longName = new string('x', 101);
        var request = new UpdateUserProfileRequest(longName);

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/users/{user.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<object?>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GetById_WhenUserExists_Returns200WithUser()
    {
        // Arrange
        var phone = "09125554433";
        var user = User.Create(phone, DateTime.UtcNow, UserRole.Admin, "مدیر سالن");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Users.AddAsync(user);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync($"/api/v1/users/{user.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<UserDto>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
        envelope.Data!.Id.Should().Be(user.Id);
        envelope.Data.PhoneNumber.Should().Be(phone);
        envelope.Data.Name.Should().Be("مدیر سالن");
        envelope.Data.Role.Should().Be("Admin");
    }

    [Fact]
    public async Task GetById_WhenUserDoesNotExist_Returns404NotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/users/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<object?>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeFalse();
    }

    private sealed record ApiResponseEnvelope<T>(T? Data, bool Success, string Message);
}
