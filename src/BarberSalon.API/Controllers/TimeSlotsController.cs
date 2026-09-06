using BarberSalon.API.Common;
using BarberSalon.Application.Booking.DTOs;
using BarberSalon.Application.Booking.Services;
using BarberSalon.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace BarberSalon.API.Controllers;

/// <summary>
/// Manages booking time slot queries and availability calculations.
/// </summary>
[ApiController]
[Route("api/v1/time-slots")]
public sealed class TimeSlotsController : ControllerBase
{
    private readonly BookingService _bookingService;

    /// <summary>
    /// Initializes a new instance of <see cref="TimeSlotsController"/>.
    /// </summary>
    /// <param name="bookingService">The application service managing booking availability.</param>
    public TimeSlotsController(BookingService bookingService)
    {
        _bookingService = bookingService;
    }

    /// <summary>
    /// Returns available booking time slots for a given date and optional staff member.
    /// </summary>
    /// <remarks>
    /// Generates standard 30-minute intervals between 09:00 and 18:00.
    /// If staffId is provided, checks specific staff availability.
    /// If staffId is omitted, checks whether at least one active staff member is free.
    /// </remarks>
    /// <param name="date">The requested booking date in ISO format (yyyy-MM-dd).</param>
    /// <param name="staffId">Optional staff member identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of time slots with availability flags wrapped in the standard response envelope.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<TimeSlotDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAvailableTimeSlots(
        [FromQuery] string? date,
        [FromQuery] Guid? staffId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var slots = await _bookingService.GetAvailableTimeSlotsAsync(date ?? string.Empty, staffId, cancellationToken);
            return Ok(ApiResponse<List<TimeSlotDto>>.CreateSuccess(slots, "Available time slots retrieved successfully."));
        }
        catch (ValidationException ex)
        {
            return BadRequest(new ApiResponse<object?>(null, false, ex.Message));
        }
        catch (NotFoundException ex)
        {
            return NotFound(new ApiResponse<object?>(null, false, ex.Message));
        }
    }
}
