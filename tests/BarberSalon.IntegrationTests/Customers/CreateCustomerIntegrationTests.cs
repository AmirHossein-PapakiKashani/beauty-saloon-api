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

public class CreateCustomerIntegrationTests : IClassFixture<BarberSalonWebFactory>
{
    private const string Endpoint = "/api/v1/customers";

    private readonly HttpClient _client;
    private readonly BarberSalonWebFactory _factory;

    public CreateCustomerIntegrationTests(BarberSalonWebFactory factory)
    {
        _factory = factory;
        _client = factory.GetClient();
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task CreateCustomer_WithValidData_Returns201CreatedAndPersistsInDatabase()
    {
        // Arrange
        var request = new CreateCustomerRequest("الهام صادقی", "09121114477", "female", "مشتری جدید");

        // Act
        var response = await _client.PostAsJsonAsync(Endpoint, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<CustomerDto>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
        envelope.Data!.Name.Should().Be("الهام صادقی");
        envelope.Data.Phone.Should().Be("09121114477");
        envelope.Data.Gender.Should().Be("female");
        envelope.Data.Notes.Should().Be("مشتری جدید");
        envelope.Data.Status.Should().Be("active");
        envelope.Data.AppointmentsCount.Should().Be(0);

        // Verify in DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var customerInDb = await db.Customers.FindAsync(envelope.Data.Id);
        customerInDb.Should().NotBeNull();
        customerInDb!.FullName.Should().Be("الهام صادقی");
        customerInDb.PhoneNumber.Should().Be("09121114477");
    }

    [Fact]
    public async Task CreateCustomer_WithDuplicatePhoneNumber_Returns400BadRequest()
    {
        // Arrange
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var existingCustomer = Customer.Create("مشتری قدیمی", "09128889900");
            db.Customers.Add(existingCustomer);
            await db.SaveChangesAsync();
        }

        var request = new CreateCustomerRequest("مشتری تکراری", "09128889900");

        // Act
        var response = await _client.PostAsJsonAsync(Endpoint, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<object?>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeFalse();
        envelope.Message.Should().Contain("already exists");
    }

    [Fact]
    public async Task CreateCustomer_WithEmptyName_Returns400BadRequest()
    {
        // Arrange
        var request = new CreateCustomerRequest("", "09129998877");

        // Act
        var response = await _client.PostAsJsonAsync(Endpoint, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<object?>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task CreateCustomer_WithEmptyPhone_Returns400BadRequest()
    {
        // Arrange
        var request = new CreateCustomerRequest("شیرین فلاح", "");

        // Act
        var response = await _client.PostAsJsonAsync(Endpoint, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<object?>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GetById_WhenCustomerExists_Returns200WithCustomer()
    {
        // Arrange
        Guid customerId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var customer = Customer.Create("پریا عباسی", "09120001122", "female", "یادداشت تست");
            db.Customers.Add(customer);
            await db.SaveChangesAsync();
            customerId = customer.Id;
        }

        // Act
        var response = await _client.GetAsync($"{Endpoint}/{customerId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<CustomerDto>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
        envelope.Data!.Id.Should().Be(customerId);
        envelope.Data.Name.Should().Be("پریا عباسی");
        envelope.Data.Phone.Should().Be("09120001122");
    }

    [Fact]
    public async Task GetById_WhenCustomerDoesNotExist_Returns404NotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"{Endpoint}/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<object?>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeFalse();
        envelope.Message.Should().Contain("not found");
    }

    private sealed record ApiResponseEnvelope<T>(T Data, bool Success, string Message);
}
