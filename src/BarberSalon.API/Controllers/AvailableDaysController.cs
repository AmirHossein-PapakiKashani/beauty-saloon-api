using BarberSalon.API.Common;
using BarberSalon.Application.Booking.DTOs;
using BarberSalon.Application.Booking.Services;
using Microsoft.AspNetCore.Mvc;

namespace BarberSalon.API.Controllers;

/// <summary>
/// Provides the list of upcoming days that customers can book.
/// </summary>
[ApiController]
[Route("api/v1/available-days")]
public sealed class AvailableDaysController : ControllerBase
{
    private readonly AvailableDaysService _availableDaysService;

    /// <summary>Creates the controller.</summary>
    /// <param name="availableDaysService">The use-case service.</param>
    public AvailableDaysController(AvailableDaysService availableDaysService)
    {
        _availableDaysService = availableDaysService;
    }

    /// <summary>Returns the next <paramref name="count"/> available booking days, starting today.</summary>
    /// <remarks>Days are consecutive and ordered ascending. The default count is 7.</remarks>
    /// <param name="count">Number of days to return (1 to 30).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of available days wrapped in the standard response envelope.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<AvailableDayDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailableDays(
        int count = AvailableDaysService.DefaultCount,
        CancellationToken cancellationToken = default)
    {
        var days = await _availableDaysService.GetAvailableDaysAsync(count, cancellationToken);
        return Ok(ApiResponse<List<AvailableDayDto>>.CreateSuccess(days, "Available days retrieved successfully."));
    }
}