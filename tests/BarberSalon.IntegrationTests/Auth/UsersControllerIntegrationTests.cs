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

public class UsersControllerIntegrationTests : IClassFixture<BarberSalonWebFactory>
{
    private readonly HttpClient _client;
    private readonly BarberSalonWebFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public UsersControllerIntegrationTests(BarberSalonWebFactory factory)
    {
        _factory = factory;
        _client = factory.GetClient();
    }

    [Fact]
    public async Task PutProfile_WithValidName_Returns200AndUpdatedName()
    {
        // Arrange
        Guid userId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = User.Create("09121110022", DateTime.UtcNow, UserRole.Customer, "نام قدیمی");
            db.Users.Add(user);
            await db.SaveChangesAsync();
            userId = user.Id;
        }

        var payload = new UpdateUserProfileRequest("نام جدید");

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/users/{userId}/profile", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<UserDto>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
        envelope.Data!.Name.Should().Be("نام جدید");

        // Verify in DB
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var dbUser = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            dbUser.Should().NotBeNull();
            dbUser!.FullName.Should().Be("نام جدید");
        }
    }

    [Fact]
    public async Task GetById_WithExistingUser_Returns200AndUserProfile()
    {
        // Arrange
        Guid userId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = User.Create("09123334455", DateTime.UtcNow, UserRole.Customer, "کاربر تستی");
            db.Users.Add(user);
            await db.SaveChangesAsync();
            userId = user.Id;
        }

        // Act
        var response = await _client.GetAsync($"/api/v1/users/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<UserDto>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
        envelope.Data!.Id.Should().Be(userId);
        envelope.Data.PhoneNumber.Should().Be("09123334455");
        envelope.Data.Name.Should().Be("کاربر تستی");
    }

    [Fact]
    public async Task PutProfile_WithNonExistentId_Returns404NotFound()
    {
        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/users/{Guid.NewGuid()}/profile", new UpdateUserProfileRequest("نام"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record ApiResponseEnvelope<T>(T? Data, bool Success, string Message);
}
