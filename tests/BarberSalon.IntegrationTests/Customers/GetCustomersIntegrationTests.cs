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

public class GetCustomersIntegrationTests : IClassFixture<BarberSalonWebFactory>
{
    private const string Endpoint = "/api/v1/customers";

    private readonly HttpClient _client;
    private readonly BarberSalonWebFactory _factory;

    public GetCustomersIntegrationTests(BarberSalonWebFactory factory)
    {
        _factory = factory;
        _client = factory.GetClient();
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task GetCustomers_WhenCustomersExist_Returns200WithAllCustomersAndStandardEnvelope()
    {
        // Arrange
        var phone1 = $"0912{Random.Shared.Next(1000000, 9999999)}";
        var phone2 = $"0912{Random.Shared.Next(1000000, 9999999)}";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var c1 = Customer.Create("کیوان رضایی", phone1, "male", "مشتری فعال ۱");
            var c2 = Customer.Create("مونا سعیدی", phone2, "female", "مشتری ۲");
            db.Customers.AddRange(c1, c2);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync(Endpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<List<CustomerDto>>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Message.Should().Be("Customers retrieved successfully.");
        envelope.Data.Should().NotBeNull();
        envelope.Data!.Should().Contain(c => c.Phone == phone1 && c.Name == "کیوان رضایی" && c.Status == "active");
        envelope.Data!.Should().Contain(c => c.Phone == phone2 && c.Name == "مونا سعیدی");
    }

    [Fact]
    public async Task GetCustomers_WhenStatusIsActive_Returns200WithOnlyActiveCustomers()
    {
        // Arrange
        var activePhone = $"0912{Random.Shared.Next(1000000, 9999999)}";
        var inactivePhone = $"0912{Random.Shared.Next(1000000, 9999999)}";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var activeCustomer = Customer.Create("سعید شریفی", activePhone, "male");
            var inactiveCustomer = Customer.Create("مهسا ابراهیمی", inactivePhone, "female");
            inactiveCustomer.Archive();

            db.Customers.AddRange(activeCustomer, inactiveCustomer);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync($"{Endpoint}?status=active");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<List<CustomerDto>>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
        envelope.Data!.Should().Contain(c => c.Phone == activePhone && c.Status == "active");
        envelope.Data!.Should().NotContain(c => c.Phone == inactivePhone);
    }

    [Fact]
    public async Task GetCustomers_WhenStatusIsInactive_Returns200WithOnlyInactiveCustomers()
    {
        // Arrange
        var activePhone = $"0912{Random.Shared.Next(1000000, 9999999)}";
        var inactivePhone = $"0912{Random.Shared.Next(1000000, 9999999)}";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var activeCustomer = Customer.Create("پیمان راد", activePhone, "male");
            var inactiveCustomer = Customer.Create("فرشته حسینی", inactivePhone, "female");
            inactiveCustomer.Archive();

            db.Customers.AddRange(activeCustomer, inactiveCustomer);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync($"{Endpoint}?status=inactive");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<List<CustomerDto>>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
        envelope.Data!.Should().Contain(c => c.Phone == inactivePhone && c.Status == "inactive");
        envelope.Data!.Should().NotContain(c => c.Phone == activePhone);
    }

    [Fact]
    public async Task GetCustomers_WhenDatabaseIsEmpty_Returns200WithEmptyListAndSuccessEnvelope()
    {
        // Act
        var response = await _client.GetAsync(Endpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<List<CustomerDto>>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
    }

    private sealed record ApiResponseEnvelope<T>(T Data, bool Success, string Message);
}
