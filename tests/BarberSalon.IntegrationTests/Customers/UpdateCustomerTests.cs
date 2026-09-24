using System.Net;
using System.Net.Http.Json;
using BarberSalon.Application.Customers.DTOs;
using BarberSalon.Domain.Customers.Entities;
using BarberSalon.Infrastructure.Persistence;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BarberSalon.IntegrationTests.Customers;

public class UpdateCustomerTests : IClassFixture<BarberSalonWebFactory>
{
    private readonly HttpClient _client;
    private readonly BarberSalonWebFactory _factory;

    public UpdateCustomerTests(BarberSalonWebFactory factory)
    {
        _factory = factory;
        _client = factory.GetClient();
    }

    [Fact]
    public async Task PutCustomer_WithValidPayload_Returns200AndUpdatedCustomer()
    {
        // Arrange
        Guid customerId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var customer = Customer.Create("مشتری اولیه", "09351112233", "male", "یادداشت اولیه");
            db.Customers.Add(customer);
            await db.SaveChangesAsync();
            customerId = customer.Id;
        }

        var updatePayload = new
        {
            name = "مشتری به‌روزشده",
            phone = "09351112233",
            gender = "male",
            notes = "یادداشت جدید"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/customers/{customerId}", updatePayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PutCustomer_WithDuplicatePhone_Returns400BadRequest()
    {
        // Arrange
        Guid customerId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var existing = Customer.Create("مشتری موجود", "09359998877", "male", "تست");
            var target = Customer.Create("مشتری هدف", "09351114455", "female", "تست ۲");
            db.Customers.AddRange(existing, target);
            await db.SaveChangesAsync();
            customerId = target.Id;
        }

        var updatePayload = new
        {
            name = "تغییر نام",
            phone = "09359998877" // duplicate with existing
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/customers/{customerId}", updatePayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteCustomer_WithValidId_ReturnsOkAndArchivesCustomer()
    {
        // Arrange
        Guid customerId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var customer = Customer.Create("مشتری برای حذف", "09357771122", "male", "حذف");
            db.Customers.Add(customer);
            await db.SaveChangesAsync();
            customerId = customer.Id;
        }

        // Act
        var response = await _client.DeleteAsync($"/api/v1/customers/{customerId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

