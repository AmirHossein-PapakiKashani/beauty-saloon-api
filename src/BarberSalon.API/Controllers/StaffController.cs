using BarberSalon.API.Common;
using BarberSalon.Application.Common.Exceptions;
using BarberSalon.Application.Staff.DTOs;
using BarberSalon.Application.Staff.Services;
using Microsoft.AspNetCore.Authorization;
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

    /// <summary>Returns all active staff members or searches by slug/service.</summary>
    /// <remarks>Public endpoint — returns active staff ordered by name, or a single staff member if slug is provided.</remarks>
    /// <param name="status">Optional filter by status (e.g. "active"). Defaults to active staff.</param>
    /// <param name="serviceId">Optional filter by offered salon service identifier.</param>
    /// <param name="slug">Optional URL-friendly slug filter to lookup a single staff member.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of active staff members or a single staff member wrapped in standard response envelope.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<StaffDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<StaffDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status = null,
        [FromQuery] Guid? serviceId = null,
        [FromQuery] string? slug = null,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(slug))
        {
            try
            {
                var found = await _staffService.GetBySlugAsync(slug.Trim(), cancellationToken);
                return Ok(ApiResponse<StaffDto>.CreateSuccess(found, "Staff member retrieved successfully."));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ApiResponse<object?>(null, false, ex.Message));
            }
        }

        if (serviceId.HasValue)
        {
            var staffByService = await _staffService.GetByServiceIdAsync(serviceId.Value, cancellationToken);
            return Ok(ApiResponse<List<StaffDto>>.CreateSuccess(staffByService, "Staff members for service retrieved successfully."));
        }

        if (string.Equals(status, "all", StringComparison.OrdinalIgnoreCase))
        {
            var allStaff = await _staffService.GetAllAsync(cancellationToken);
            return Ok(ApiResponse<List<StaffDto>>.CreateSuccess(allStaff, "All staff members retrieved successfully."));
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

    /// <summary>Creates a new staff member.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<StaffDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateStaffRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var created = await _staffService.CreateAsync(request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, ApiResponse<StaffDto>.CreateSuccess(created, "Staff member created successfully."));
        }
        catch (ValidationException ex)
        {
            return BadRequest(new ApiResponse<object?>(null, false, ex.Message));
        }
    }

    /// <summary>Updates an existing staff member.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<StaffDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateStaffRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var updated = await _staffService.UpdateAsync(id, request, cancellationToken);
            return Ok(ApiResponse<StaffDto>.CreateSuccess(updated, "Staff member updated successfully."));
        }
        catch (NotFoundException ex)
        {
            return NotFound(new ApiResponse<object?>(null, false, ex.Message));
        }
        catch (ValidationException ex)
        {
            return BadRequest(new ApiResponse<object?>(null, false, ex.Message));
        }
    }

    /// <summary>Archives (soft deletes) a staff member.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _staffService.ArchiveAsync(id, cancellationToken);
            return Ok(ApiResponse<object?>.CreateSuccess(null, "Staff member archived successfully."));
        }
        catch (NotFoundException ex)
        {
            return NotFound(new ApiResponse<object?>(null, false, ex.Message));
        }
    }

    /// <summary>Reactivates an archived staff member.</summary>
    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _staffService.ActivateAsync(id, cancellationToken);
            return Ok(ApiResponse<object?>.CreateSuccess(null, "Staff member activated successfully."));
        }
        catch (NotFoundException ex)
        {
            return NotFound(new ApiResponse<object?>(null, false, ex.Message));
        }
    }

    /// <summary>Returns the authenticated staff member's profile.</summary>
    [HttpGet("me")]
    [Authorize(Policy = "StaffOrAdmin")]
    [ProducesResponseType(typeof(ApiResponse<StaffDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken = default)
    {
        var staffDto = await ResolveCurrentStaffAsync(cancellationToken);
        if (staffDto == null)
        {
            return NotFound(new ApiResponse<object?>(null, false, "پروفایل پرسنل برای کاربر جاری یافت نشد."));
        }

        return Ok(ApiResponse<StaffDto>.CreateSuccess(staffDto, "پروفایل پرسنل با موفقیت دریافت شد."));
    }

    /// <summary>Allows a staff member to self-update their bio and avatar.</summary>
    [HttpPatch("me/profile")]
    [HttpPut("me/profile")]
    [Authorize(Policy = "StaffOrAdmin")]
    [ProducesResponseType(typeof(ApiResponse<StaffDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSelfProfile(
        [FromBody] UpdateStaffSelfProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var staffDto = await ResolveCurrentStaffAsync(cancellationToken);
        if (staffDto == null)
        {
            return NotFound(new ApiResponse<object?>(null, false, "پروفایل پرسنل برای کاربر جاری یافت نشد."));
        }

        try
        {
            var updated = await _staffService.UpdateSelfProfileAsync(staffDto.Id, request, cancellationToken);
            return Ok(ApiResponse<StaffDto>.CreateSuccess(updated, "پروفایل پرسنل با موفقیت ویرایش شد."));
        }
        catch (ValidationException ex)
        {
            return BadRequest(new ApiResponse<object?>(null, false, ex.Message));
        }
    }

    /// <summary>Returns earnings, completed appointments, and commissions for the authenticated staff member.</summary>
    [HttpGet("me/earnings")]
    [Authorize(Policy = "StaffOrAdmin")]
    [ProducesResponseType(typeof(ApiResponse<StaffEarningsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyEarnings(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var staffDto = await ResolveCurrentStaffAsync(cancellationToken);
        if (staffDto == null)
        {
            return NotFound(new ApiResponse<object?>(null, false, "پروفایل پرسنل برای کاربر جاری یافت نشد."));
        }

        var earnings = await _staffService.GetStaffEarningsAsync(staffDto.Id, startDate, endDate, cancellationToken);
        return Ok(ApiResponse<StaffEarningsDto>.CreateSuccess(earnings, "گزارش درآمد پرسنل با موفقیت دریافت شد."));
    }

    /// <summary>Returns earnings and commissions for a specific staff member. Admin only.</summary>
    [HttpGet("{id:guid}/earnings")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<StaffEarningsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStaffEarnings(
        Guid id,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var earnings = await _staffService.GetStaffEarningsAsync(id, startDate, endDate, cancellationToken);
            return Ok(ApiResponse<StaffEarningsDto>.CreateSuccess(earnings, "گزارش درآمد پرسنل با موفقیت دریافت شد."));
        }
        catch (NotFoundException ex)
        {
            return NotFound(new ApiResponse<object?>(null, false, ex.Message));
        }
    }

    private async Task<StaffDto?> ResolveCurrentStaffAsync(CancellationToken cancellationToken)
    {
        var staffIdClaim = User.FindFirst("StaffId")?.Value ?? User.FindFirst("staff_id")?.Value;
        if (!string.IsNullOrWhiteSpace(staffIdClaim) && Guid.TryParse(staffIdClaim, out var staffGuid))
        {
            try
            {
                return await _staffService.GetByIdAsync(staffGuid, cancellationToken);
            }
            catch (NotFoundException) { }
        }

        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        if (!string.IsNullOrWhiteSpace(userIdStr) && Guid.TryParse(userIdStr, out var userId))
        {
            var staff = await _staffService.GetByUserIdAsync(userId, cancellationToken);
            if (staff != null) return staff;
        }

        var phone = User.FindFirst(System.Security.Claims.ClaimTypes.MobilePhone)?.Value;
        if (!string.IsNullOrWhiteSpace(phone))
        {
            var staff = await _staffService.GetByPhoneNumberAsync(phone, cancellationToken);
            if (staff != null) return staff;
        }

        return null;
    }
}

