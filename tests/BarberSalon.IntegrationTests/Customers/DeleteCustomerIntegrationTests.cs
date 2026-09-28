using System.Net;
using BarberSalon.Domain.Customers.Entities;
using BarberSalon.Infrastructure.Persistence;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BarberSalon.IntegrationTests.Customers;

public class DeleteCustomerIntegrationTests : IClassFixture<BarberSalonWebFactory>
{
    private readonly HttpClient _client;
    private readonly BarberSalonWebFactory _factory;

    public DeleteCustomerIntegrationTests(BarberSalonWebFactory factory)
    {
        _factory = factory;
        _client = factory.GetClient();
    }

    [Fact]
    public async Task DeleteCustomer_WithExistingCustomer_Returns200AndArchivesCustomer()
    {
        // Arrange
        var customer = Customer.Create("مشتری حذفی", "09129876543");
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Customers.Add(customer);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.DeleteAsync($"/api/v1/customers/{customer.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var archived = await db.Customers.FindAsync(customer.Id);
            archived.Should().NotBeNull();
            archived!.IsActive.Should().BeFalse();
        }
    }

    [Fact]
    public async Task DeleteCustomer_WithNonExistentId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/v1/customers/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
