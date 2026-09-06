using System.Net;
using BarberSalon.API;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BarberSalon.IntegrationTests;

public class CorsIntegrationTests : IClassFixture<BarberSalonWebFactory>
{
    private readonly BarberSalonWebFactory _factory;

    public CorsIntegrationTests(BarberSalonWebFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void CorsPolicy_IsRegisteredWithExpectedSettings()
    {
        // Assert CorsPolicyName constant
        DependencyInjection.CorsPolicyName.Should().Be("AllowFrontend");

        using var scope = _factory.Services.CreateScope();
        var corsOptions = scope.ServiceProvider.GetRequiredService<IOptions<CorsOptions>>().Value;
        var policy = corsOptions.GetPolicy(DependencyInjection.CorsPolicyName);

        policy.Should().NotBeNull("AllowFrontend CORS policy must be registered");
        policy!.Origins.Should().Contain("http://localhost:3000");
        policy.SupportsCredentials.Should().BeTrue();
        policy.AllowAnyHeader.Should().BeTrue();
        policy.AllowAnyMethod.Should().BeTrue();
    }

    [Fact]
    public async Task PreflightRequest_FromAllowedOrigin_ReturnsCorsHeaders()
    {
        var client = _factory.GetClient();

        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/services");
        request.Headers.Add("Origin", "http://localhost:3000");
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeTrue();
        response.Headers.GetValues("Access-Control-Allow-Origin").Should().Contain("http://localhost:3000");
        response.Headers.Contains("Access-Control-Allow-Credentials").Should().BeTrue();
        response.Headers.GetValues("Access-Control-Allow-Credentials").Should().Contain("true");
    }

    [Fact]
    public async Task GetRequest_FromAllowedOrigin_IncludesCorsHeaders()
    {
        var client = _factory.GetClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/services");
        request.Headers.Add("Origin", "http://localhost:3000");

        var response = await client.SendAsync(request);

        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeTrue();
        response.Headers.GetValues("Access-Control-Allow-Origin").Should().Contain("http://localhost:3000");
        response.Headers.Contains("Access-Control-Allow-Credentials").Should().BeTrue();
        response.Headers.GetValues("Access-Control-Allow-Credentials").Should().Contain("true");
    }
}
