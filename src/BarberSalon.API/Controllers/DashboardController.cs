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

    /// <summary>
    /// Returns recent salon activity feed for the administrator dashboard.
    /// </summary>
    [HttpGet("activities")]
    [ProducesResponseType(typeof(ApiResponse<List<ActivityLogDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActivities([FromQuery] int count = 10, CancellationToken cancellationToken = default)
    {
        var activities = await _dashboardService.GetActivitiesAsync(count, cancellationToken);
        return Ok(ApiResponse<List<ActivityLogDto>>.CreateSuccess(activities, "Recent activities retrieved successfully."));
    }

    /// <summary>
    /// Returns revenue forecast for the current rolling 7-day window.
    /// </summary>
    [HttpGet("revenue-forecast")]
    [ProducesResponseType(typeof(ApiResponse<RevenueForecastDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRevenueForecast(CancellationToken cancellationToken = default)
    {
        var forecast = await _dashboardService.GetRevenueForecastAsync(cancellationToken);
        return Ok(ApiResponse<RevenueForecastDto>.CreateSuccess(forecast, "Revenue forecast retrieved successfully."));
    }

    /// <summary>
    /// Returns customers at risk of churning.
    /// </summary>
    [HttpGet("at-risk-customers")]
    [ProducesResponseType(typeof(ApiResponse<List<AtRiskCustomerDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAtRiskCustomers(CancellationToken cancellationToken = default)
    {
        var atRisk = await _dashboardService.GetAtRiskCustomersAsync(cancellationToken);
        return Ok(ApiResponse<List<AtRiskCustomerDto>>.CreateSuccess(atRisk, "At-risk customers retrieved successfully."));
    }

    /// <summary>
    /// Returns schedule gaps for the current week.
    /// </summary>
    [HttpGet("gap-analysis")]
    [ProducesResponseType(typeof(ApiResponse<List<TimeGapDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGapAnalysis([FromQuery] int maxGaps = 12, CancellationToken cancellationToken = default)
    {
        var gaps = await _dashboardService.GetGapAnalysisAsync(maxGaps, cancellationToken);
        return Ok(ApiResponse<List<TimeGapDto>>.CreateSuccess(gaps, "Gap analysis retrieved successfully."));
    }
}
