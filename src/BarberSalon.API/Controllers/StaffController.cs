using BarberSalon.API.Common;
using BarberSalon.Application.Staff.DTOs;
using BarberSalon.Application.Staff.Services;
using Microsoft.AspNetCore.Mvc;

namespace BarberSalon.API.Controllers;

/// <summary>
/// Manages salon staff members (barbers, stylists, colorists).
/// </summary>
[ApiController]
[Route("api/v1/staff")]
public sealed class StaffController : ControllerBase
{
    private readonly StaffService _staffService;

    /// <summary>Creates a new instance of <see cref="StaffController"/>.</summary>
    /// <param name="staffService">Application service for staff operations.</param>
    public StaffController(StaffService staffService)
    {
        _staffService = staffService;
    }

    /// <summary>Returns all active staff members.</summary>
    /// <remarks>Public endpoint — returns active staff ordered by name.</remarks>
    /// <param name="status">Optional filter by status (e.g. "active"). Defaults to active staff.</param>
    /// <param name="serviceId">Optional filter by offered salon service identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of active staff members wrapped in standard response envelope.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<StaffDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status = null,
        [FromQuery] Guid? serviceId = null,
        CancellationToken cancellationToken = default)
    {
        if (serviceId.HasValue)
        {
            var staffByService = await _staffService.GetByServiceIdAsync(serviceId.Value, cancellationToken);
            return Ok(ApiResponse<List<StaffDto>>.CreateSuccess(staffByService, "Staff members for service retrieved successfully."));
        }

        var staff = await _staffService.GetAllActiveAsync(cancellationToken);
        return Ok(ApiResponse<List<StaffDto>>.CreateSuccess(staff, "Active staff members retrieved successfully."));
    }

    /// <summary>Returns a single staff member by their unique identifier.</summary>
    /// <param name="id">The unique identifier of the staff member.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The staff member details wrapped in standard response envelope.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<StaffDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var staffMember = await _staffService.GetByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<StaffDto>.CreateSuccess(staffMember, "Staff member retrieved successfully."));
    }
}
