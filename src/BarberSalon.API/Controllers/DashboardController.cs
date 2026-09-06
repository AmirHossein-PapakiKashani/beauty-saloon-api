using BarberSalon.API.Common;
using BarberSalon.Application.Admin.DTOs;
using BarberSalon.Application.Admin.Services;
using Microsoft.AspNetCore.Mvc;

namespace BarberSalon.API.Controllers;

/// <summary>
/// Provides administrative and operational statistics for the salon management dashboard.
/// </summary>
[ApiController]
[Route("api/v1/dashboard")]
[Route("api/v1/dashboard-stats")]
[Route("api/v1/admin/dashboard-stats")]
[Route("api/v1/admin/dashboard/stats")]
public sealed class DashboardController : ControllerBase
{
    private readonly DashboardService _dashboardService;

    /// <summary>
    /// Initializes a new instance of <see cref="DashboardController"/>.
    /// </summary>
    /// <param name="dashboardService">Application service for computing dashboard metrics.</param>
    public DashboardController(DashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>
    /// Returns aggregated high-level business metrics for the salon administrator dashboard.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The aggregated dashboard statistics wrapped in the standard API response envelope.</returns>
    [HttpGet]
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiResponse<DashboardStatsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboardStats(CancellationToken cancellationToken = default)
    {
        var stats = await _dashboardService.GetDashboardStatsAsync(cancellationToken);
        return Ok(ApiResponse<DashboardStatsDto>.CreateSuccess(stats, "Dashboard statistics retrieved successfully."));
    }
}
