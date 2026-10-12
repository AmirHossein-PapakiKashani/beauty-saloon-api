using BarberSalon.API.Common;
using BarberSalon.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BarberSalon.API.Controllers;

/// <summary>
/// DTO representing database connectivity and status.
/// </summary>
public sealed record DatabaseHealthDto(
    string Status,
    string Provider,
    bool CanConnect
);

/// <summary>
/// DTO representing overall health and readiness of the service.
/// </summary>
public sealed record HealthStatusDto(
    string Status,
    DatabaseHealthDto Database,
    string Environment,
    DateTime TimestampUtc
);

/// <summary>
/// Exposes operational health check endpoints for liveness, readiness, and monitoring.
/// </summary>
[ApiController]
[AllowAnonymous]
public sealed class HealthController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IHostEnvironment _env;

    /// <summary>
    /// Initializes a new instance of <see cref="HealthController"/>.
    /// </summary>
    /// <param name="dbContext">Application database context.</param>
    /// <param name="env">Hosting environment information.</param>
    public HealthController(AppDbContext dbContext, IHostEnvironment env)
    {
        _dbContext = dbContext;
        _env = env;
    }

    /// <summary>
    /// Checks the overall health of the API and its backing services.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>System health report including database status and environment details.</returns>
    [HttpGet("health")]
    [HttpGet("api/v1/health")]
    [ProducesResponseType(typeof(ApiResponse<HealthStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<HealthStatusDto>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetHealth(CancellationToken cancellationToken = default)
    {
        bool dbCanConnect = false;
        string provider = "Unknown";
        try
        {
            provider = _dbContext.Database.ProviderName ?? "Unknown";
            dbCanConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
        }
        catch
        {
            dbCanConnect = false;
        }

        var dbHealth = new DatabaseHealthDto(
            Status: dbCanConnect ? "Healthy" : "Unhealthy",
            Provider: provider,
            CanConnect: dbCanConnect
        );

        var isHealthy = dbCanConnect;
        var healthDto = new HealthStatusDto(
            Status: isHealthy ? "Healthy" : "Unhealthy",
            Database: dbHealth,
            Environment: _env.EnvironmentName,
            TimestampUtc: DateTime.UtcNow
        );

        if (!isHealthy)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new ApiResponse<HealthStatusDto>(healthDto, false, "Service is unhealthy: Database connection failed."));
        }

        return Ok(ApiResponse<HealthStatusDto>.CreateSuccess(healthDto, "Service is healthy."));
    }
}
