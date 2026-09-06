using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BarberSalon.Application.Customers.DTOs;
using BarberSalon.Domain.Customers.Entities;
using BarberSalon.Infrastructure.Persistence;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BarberSalon.IntegrationTests.Customers;

public class SearchCustomersIntegrationTests : IClassFixture<BarberSalonWebFactory>
{
    private const string SearchEndpoint = "/api/v1/customers/search";
    private const string BaseEndpoint = "/api/v1/customers";

    private readonly HttpClient _client;
    private readonly BarberSalonWebFactory _factory;

    public SearchCustomersIntegrationTests(BarberSalonWebFactory factory)
    {
        _factory = factory;
        _client = factory.GetClient();
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task SearchCustomers_WhenQueryMatchesName_Returns200WithMatchingCustomersAndEnvelope()
    {
        // Arrange
        var phone1 = $"0912{Random.Shared.Next(1000000, 9999999)}";
        var phone2 = $"0912{Random.Shared.Next(1000000, 9999999)}";
        var uniqueName = $"جستجو_نام_{Guid.NewGuid():N}";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var c1 = Customer.Create(uniqueName, phone1, "male", "مشتری تست جستجو");
            var c2 = Customer.Create($"دیگر_{Guid.NewGuid():N}", phone2, "female");
            db.Customers.AddRange(c1, c2);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync($"{SearchEndpoint}?query={uniqueName}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<List<CustomerDto>>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Message.Should().Be("Customers search completed successfully.");
        envelope.Data.Should().NotBeNull();
        envelope.Data!.Should().Contain(c => c.Name == uniqueName && c.Phone == phone1);
        envelope.Data!.Should().NotContain(c => c.Phone == phone2);
    }

    [Fact]
    public async Task SearchCustomers_WhenQueryMatchesPhone_Returns200WithMatchingCustomersAndEnvelope()
    {
        // Arrange
        var uniquePhone = $"0999{Random.Shared.Next(1000000, 9999999)}";
        var otherPhone = $"0988{Random.Shared.Next(1000000, 9999999)}";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var c1 = Customer.Create("مشتری تلفنی ۱", uniquePhone, "male");
            var c2 = Customer.Create("مشتری تلفنی ۲", otherPhone, "female");
            db.Customers.AddRange(c1, c2);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync($"{SearchEndpoint}?query={uniquePhone}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<List<CustomerDto>>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
        envelope.Data!.Should().Contain(c => c.Phone == uniquePhone && c.Name == "مشتری تلفنی ۱");
        envelope.Data!.Should().NotContain(c => c.Phone == otherPhone);
    }

    [Fact]
    public async Task SearchCustomers_WhenQueryMatchesNothing_Returns200WithEmptyList()
    {
        // Act
        var response = await _client.GetAsync($"{SearchEndpoint}?query=نام_ناموجود_غیرقابل_یافت_{Guid.NewGuid():N}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<List<CustomerDto>>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
        envelope.Data!.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchCustomers_WhenQueryIsEmpty_Returns200WithAllActiveCustomers()
    {
        // Arrange
        var phone = $"0912{Random.Shared.Next(1000000, 9999999)}";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var c1 = Customer.Create("مشتری فعال خالی", phone);
            db.Customers.Add(c1);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync(SearchEndpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<List<CustomerDto>>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
        envelope.Data!.Should().Contain(c => c.Phone == phone);
    }

    [Fact]
    public async Task SearchCustomers_WhenCalledOnRootEndpointWithQueryParam_Returns200WithMatchingCustomers()
    {
        // Arrange
        var phone = $"0912{Random.Shared.Next(1000000, 9999999)}";
        var uniqueName = $"جستجو_رووت_{Guid.NewGuid():N}";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var c1 = Customer.Create(uniqueName, phone);
            db.Customers.Add(c1);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync($"{BaseEndpoint}?query={uniqueName}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<List<CustomerDto>>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Message.Should().Be("Customers search completed successfully.");
        envelope.Data.Should().NotBeNull();
        envelope.Data!.Should().Contain(c => c.Name == uniqueName && c.Phone == phone);
    }

    [Fact]
    public async Task SearchCustomers_WhenCustomerIsInactive_ExcludesArchivedCustomerFromSearch()
    {
        // Arrange
        var activePhone = $"0912{Random.Shared.Next(1000000, 9999999)}";
        var inactivePhone = $"0912{Random.Shared.Next(1000000, 9999999)}";
        var sharedTerm = $"تست_بایگانی_{Guid.NewGuid():N}";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var active = Customer.Create($"{sharedTerm} فعال", activePhone);
            var inactive = Customer.Create($"{sharedTerm} غیرفعال", inactivePhone);
            inactive.Archive();
            db.Customers.AddRange(active, inactive);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync($"{SearchEndpoint}?query={sharedTerm}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<List<CustomerDto>>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
        envelope.Data!.Should().Contain(c => c.Phone == activePhone);
        envelope.Data!.Should().NotContain(c => c.Phone == inactivePhone);
    }

    private sealed record ApiResponseEnvelope<T>(T Data, bool Success, string Message);
}
