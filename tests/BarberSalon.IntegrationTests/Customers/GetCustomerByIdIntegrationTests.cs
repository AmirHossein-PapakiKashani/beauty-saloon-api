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

public class GetCustomerByIdIntegrationTests : IClassFixture<BarberSalonWebFactory>
{
    private const string Endpoint = "/api/v1/customers";

    private readonly HttpClient _client;
    private readonly BarberSalonWebFactory _factory;

    public GetCustomerByIdIntegrationTests(BarberSalonWebFactory factory)
    {
        _factory = factory;
        _client = factory.GetClient();
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task GetById_WhenCustomerExists_Returns200WithCustomerAndEnvelope()
    {
        // Arrange
        Guid customerId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var customer = Customer.Create("مریم رضایی", "09123334466", "female", "یادداشت مشتری تست");
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
        envelope.Message.Should().Be("Customer retrieved successfully.");
        envelope.Data.Should().NotBeNull();
        envelope.Data!.Id.Should().Be(customerId);
        envelope.Data.Name.Should().Be("مریم رضایی");
        envelope.Data.Phone.Should().Be("09123334466");
        envelope.Data.Gender.Should().Be("female");
        envelope.Data.Notes.Should().Be("یادداشت مشتری تست");
        envelope.Data.Status.Should().Be("active");
        envelope.Data.AppointmentsCount.Should().Be(0);
        envelope.Data.LastAppointment.Should().BeNull();
    }

    [Fact]
    public async Task GetById_WhenCustomerDoesNotExist_Returns404NotFoundWithStandardEnvelope()
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
        envelope.Message.Should().Contain("Customer with key");
        envelope.Message.Should().Contain(nonExistentId.ToString());
    }

    [Fact]
    public async Task GetById_WhenCustomerIsArchived_Returns200WithInactiveStatus()
    {
        // Arrange
        Guid customerId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var customer = Customer.Create("زهرا اسدی", "09125559988", "female", "مشتری غیرفعال");
            customer.Archive();
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
        envelope.Data.Status.Should().Be("inactive");
    }

    [Fact]
    public async Task GetById_WhenCustomerHasAppointments_ReturnsCorrectAppointmentCountAndLastDate()
    {
        // Arrange
        Guid customerId;
        var appointmentTime = new DateTime(2026, 9, 1, 15, 30, 0, DateTimeKind.Utc);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var customer = Customer.Create("نرگس کمالی", "09127773344", "female", "دارای رزرو قبلی");
            customer.RecordAppointment(appointmentTime);
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
        envelope.Data.AppointmentsCount.Should().Be(1);
        envelope.Data.LastAppointment.Should().BeCloseTo(appointmentTime, TimeSpan.FromSeconds(1));
    }

    private sealed record ApiResponseEnvelope<T>(T Data, bool Success, string Message);
}
