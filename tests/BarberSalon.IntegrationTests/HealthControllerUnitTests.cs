using System.Net;
using BarberSalon.API.Common;
using BarberSalon.API.Controllers;
using BarberSalon.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace BarberSalon.IntegrationTests;

public class HealthControllerUnitTests
{
    private class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Production";
        public string ApplicationName { get; set; } = "BarberSalon";
        public string ContentRootPath { get; set; } = "C:\\";
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    [Fact]
    public async Task GetHealth_WhenDatabaseConnected_Returns200WithHealthyStatus()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new AppDbContext(options);
        var env = new FakeHostEnvironment { EnvironmentName = "Production" };
        var controller = new HealthController(dbContext, env);

        // Act
        var result = await controller.GetHealth();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        var response = okResult.Value.Should().BeOfType<ApiResponse<HealthStatusDto>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Status.Should().Be("Healthy");
        response.Data.Environment.Should().Be("Production");
        response.Data.Database.Status.Should().Be("Healthy");
        response.Data.Database.CanConnect.Should().BeTrue();
    }

    [Fact]
    public async Task GetHealth_WhenDatabaseThrowsException_Returns503WithUnhealthyStatus()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var dbContext = new AppDbContext(options);
        // Dispose DbContext so CanConnectAsync throws ObjectDisposedException
        dbContext.Dispose();

        var env = new FakeHostEnvironment { EnvironmentName = "Production" };
        var controller = new HealthController(dbContext, env);

        // Act
        var result = await controller.GetHealth();

        // Assert
        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);

        var response = statusResult.Value.Should().BeOfType<ApiResponse<HealthStatusDto>>().Subject;
        response.Success.Should().BeFalse();
        response.Data.Status.Should().Be("Unhealthy");
        response.Data.Database.Status.Should().Be("Unhealthy");
        response.Data.Database.CanConnect.Should().BeFalse();
    }
}
