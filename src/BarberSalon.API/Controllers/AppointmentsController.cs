using BarberSalon.API.Common;
using BarberSalon.Application.Booking.DTOs;
using BarberSalon.Application.Booking.Services;
using BarberSalon.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace BarberSalon.API.Controllers;

/// <summary>
/// Manages appointment bookings, reservations, and appointment lifecycle operations.
/// </summary>
[ApiController]
[Route("api/v1/appointments")]
public sealed class AppointmentsController : ControllerBase
{
    private readonly BookingService _bookingService;

    /// <summary>
    /// Initializes a new instance of <see cref="AppointmentsController"/>.
    /// </summary>
    /// <param name="bookingService">Application service for booking operations.</param>
    public AppointmentsController(BookingService bookingService)
    {
        _bookingService = bookingService;
    }

    /// <summary>
    /// Returns appointments filtered by customer ID, date, or other query parameters.
    /// </summary>
    /// <param name="customerId">Optional customer identifier filter.</param>
    /// <param name="date">Optional date filter in yyyy-MM-dd format.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of appointments wrapped in the standard response envelope.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<AppointmentDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAppointments(
        [FromQuery] Guid? customerId = null,
        [FromQuery] string? date = null,
        CancellationToken cancellationToken = default)
    {
        if (customerId.HasValue)
        {
            try
            {
                var appointments = await _bookingService.GetAppointmentsByCustomerIdAsync(customerId.Value, cancellationToken);
                return Ok(ApiResponse<List<AppointmentDto>>.CreateSuccess(appointments, "Customer appointments retrieved successfully."));
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

        if (!string.IsNullOrWhiteSpace(date))
        {
            try
            {
                var appointments = await _bookingService.GetAppointmentsByDateAsync(date, cancellationToken);
                return Ok(ApiResponse<List<AppointmentDto>>.CreateSuccess(appointments, "Appointments for date retrieved successfully."));
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ApiResponse<object?>(null, false, ex.Message));
            }
        }

        var allAppointments = await _bookingService.GetAllAppointmentsAsync(cancellationToken);
        return Ok(ApiResponse<List<AppointmentDto>>.CreateSuccess(allAppointments, "Appointments retrieved successfully."));
    }

    /// <summary>
    /// Returns all appointments for a specified date in yyyy-MM-dd format.
    /// </summary>
    /// <param name="date">The target date in yyyy-MM-dd format.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The appointments for the date wrapped in the standard response envelope.</returns>
    [HttpGet("date/{date}")]
    [ProducesResponseType(typeof(ApiResponse<List<AppointmentDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByDate(
        string date,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var appointments = await _bookingService.GetAppointmentsByDateAsync(date, cancellationToken);
            return Ok(ApiResponse<List<AppointmentDto>>.CreateSuccess(appointments, "Appointments for date retrieved successfully."));
        }
        catch (ValidationException ex)
        {
            return BadRequest(new ApiResponse<object?>(null, false, ex.Message));
        }
    }

    /// <summary>
    /// Returns all appointments for a specified customer identifier.
    /// </summary>
    /// <param name="customerId">The unique identifier of the customer.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The customer appointments wrapped in the standard response envelope.</returns>
    [HttpGet("customer/{customerId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<List<AppointmentDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByCustomer(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var appointments = await _bookingService.GetAppointmentsByCustomerIdAsync(customerId, cancellationToken);
            return Ok(ApiResponse<List<AppointmentDto>>.CreateSuccess(appointments, "Customer appointments retrieved successfully."));
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

    /// <summary>
    /// Creates a new appointment for a customer with a specified staff member and salon service.
    /// </summary>
    /// <param name="request">The appointment creation payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created appointment and unique booking code wrapped in the standard response envelope.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CreateAppointmentResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(
        [FromBody] CreateAppointmentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _bookingService.CreateAppointmentAsync(request, cancellationToken);
            return CreatedAtAction(
                nameof(GetById),
                new { id = result.Appointment.Id },
                ApiResponse<CreateAppointmentResponse>.CreateSuccess(result, "Appointment created successfully."));
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

    /// <summary>
    /// Returns details for an appointment by its unique identifier.
    /// </summary>
    /// <param name="id">The appointment GUID identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The appointment details wrapped in the standard response envelope.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AppointmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var appointment = await _bookingService.GetByIdAsync(id, cancellationToken);
            return Ok(ApiResponse<AppointmentDto>.CreateSuccess(appointment, "Appointment retrieved successfully."));
        }
        catch (NotFoundException ex)
        {
            return NotFound(new ApiResponse<object?>(null, false, ex.Message));
        }
    }
}
