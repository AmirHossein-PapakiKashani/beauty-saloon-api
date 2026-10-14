using BarberSalon.API.Common;
using BarberSalon.Application.Common.Exceptions;
using BarberSalon.Application.Staff.DTOs;
using BarberSalon.Application.Staff.Interfaces;
using BarberSalon.Application.Staff.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BarberSalon.API.Controllers;

/// <summary>
/// Manages staff leave requests (submission, personal history, and admin review).
/// </summary>
[ApiController]
[Route("api/v1/staff/leaves")]
public sealed class StaffLeaveController : ControllerBase
{
    private readonly IStaffLeaveService _staffLeaveService;
    private readonly StaffService _staffService;

    public StaffLeaveController(IStaffLeaveService staffLeaveService, StaffService staffService)
    {
        _staffLeaveService = staffLeaveService;
        _staffService = staffService;
    }

    /// <summary>Submits a new leave request for the authenticated staff member.</summary>
    [HttpPost]
    [HttpPost("me")]
    [HttpPost("/api/v1/staff/me/leaves")]
    [Authorize(Policy = "StaffOrAdmin")]
    [ProducesResponseType(typeof(ApiResponse<StaffLeaveDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmitLeave(
        [FromBody] SubmitLeaveRequest request,
        CancellationToken cancellationToken = default)
    {
        var staffDto = await ResolveCurrentStaffAsync(cancellationToken);
        if (staffDto == null)
        {
            return NotFound(new ApiResponse<object?>(null, false, "پروفایل پرسنل برای کاربر جاری یافت نشد."));
        }

        try
        {
            var result = await _staffLeaveService.SubmitLeaveAsync(staffDto.Id, request, cancellationToken);
            return Ok(ApiResponse<StaffLeaveDto>.CreateSuccess(result, "درخواست مرخصی با موفقیت ثبت شد و در انتظار تایید مدیریت است."));
        }
        catch (ValidationException ex)
        {
            return BadRequest(new ApiResponse<object?>(null, false, ex.Message));
        }
    }

    /// <summary>Returns all leave requests submitted by the authenticated staff member.</summary>
    [HttpGet]
    [HttpGet("me")]
    [HttpGet("/api/v1/staff/me/leaves")]
    [Authorize(Policy = "StaffOrAdmin")]
    [ProducesResponseType(typeof(ApiResponse<List<StaffLeaveDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyLeaves(CancellationToken cancellationToken = default)
    {
        var staffDto = await ResolveCurrentStaffAsync(cancellationToken);
        if (staffDto == null)
        {
            return NotFound(new ApiResponse<object?>(null, false, "پروفایل پرسنل برای کاربر جاری یافت نشد."));
        }

        var leaves = await _staffLeaveService.GetStaffLeavesAsync(staffDto.Id, cancellationToken);
        return Ok(ApiResponse<List<StaffLeaveDto>>.CreateSuccess(leaves, "لیست درخواست‌های مرخصی دریافت شد."));
    }

    /// <summary>Returns all pending leave requests requiring admin approval. Admin only.</summary>
    [HttpGet("admin/pending")]
    [HttpGet("/api/v1/staff/admin/leaves/pending")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<List<StaffLeaveDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingLeaves(CancellationToken cancellationToken = default)
    {
        var pending = await _staffLeaveService.GetPendingLeavesAsync(cancellationToken);
        return Ok(ApiResponse<List<StaffLeaveDto>>.CreateSuccess(pending, "درخواست‌های مرخصی در انتظار بررسی با موفقیت دریافت شدند."));
    }

    /// <summary>Approves or rejects a staff leave request. Admin only.</summary>
    [HttpPut("admin/{id:guid}/review")]
    [HttpPut("/api/v1/staff/admin/leaves/{id:guid}/review")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<StaffLeaveDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReviewLeave(
        Guid id,
        [FromBody] ReviewLeaveRequest request,
        CancellationToken cancellationToken = default)
    {
        var reviewerId = Guid.Empty;
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        if (!string.IsNullOrWhiteSpace(userIdStr) && Guid.TryParse(userIdStr, out var parsedUserId))
        {
            reviewerId = parsedUserId;
        }

        try
        {
            var result = await _staffLeaveService.ReviewLeaveAsync(id, request, reviewerId, cancellationToken);
            var actionMsg = request.Approved ? "تایید" : "رد";
            return Ok(ApiResponse<StaffLeaveDto>.CreateSuccess(result, $"درخواست مرخصی با موفقیت {actionMsg} شد."));
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
