using BarberSalon.API.Common;
using BarberSalon.Application.PlatformAdmin.DTOs;
using BarberSalon.Application.PlatformAdmin.Services;
using BarberSalon.Domain.Auth.Enums;
using Microsoft.AspNetCore.Mvc;

namespace BarberSalon.API.Controllers;

[ApiController]
[Route("api/v1/platform-admin")]
public sealed class PlatformAdminController : ControllerBase
{
    private readonly PlatformAdminService _service;

    public PlatformAdminController(PlatformAdminService service)
    {
        _service = service;
    }

    [HttpGet("metrics")]
    [ProducesResponseType(typeof(ApiResponse<PlatformMetricsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMetrics(CancellationToken ct)
    {
        var metrics = await _service.GetMetricsAsync(ct);
        return Ok(ApiResponse<PlatformMetricsDto>.CreateSuccess(metrics, "Platform metrics retrieved successfully."));
    }

    [HttpGet("users")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PlatformUserDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? search,
        [FromQuery] UserRole? role,
        [FromQuery] bool? isActive,
        CancellationToken ct)
    {
        var users = await _service.GetUsersAsync(search, role, isActive, ct);
        return Ok(ApiResponse<IReadOnlyList<PlatformUserDto>>.CreateSuccess(users, "Platform users retrieved successfully."));
    }

    [HttpPatch("users/{id:guid}/role")]
    [ProducesResponseType(typeof(ApiResponse<PlatformUserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateUserRole(
        Guid id,
        [FromBody] UpdatePlatformUserRoleRequest request,
        CancellationToken ct)
    {
        var updated = await _service.UpdateUserRoleAsync(id, request.NewRole, ct);
        return Ok(ApiResponse<PlatformUserDto>.CreateSuccess(updated, "User role updated successfully."));
    }

    [HttpPatch("users/{id:guid}/status")]
    [ProducesResponseType(typeof(ApiResponse<PlatformUserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ToggleUserStatus(
        Guid id,
        [FromBody] ToggleStatusRequest request,
        CancellationToken ct)
    {
        var updated = await _service.ToggleUserStatusAsync(id, request.IsActive, ct);
        return Ok(ApiResponse<PlatformUserDto>.CreateSuccess(updated, "User status updated successfully."));
    }

    [HttpGet("salons")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PlatformSalonDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSalons(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        CancellationToken ct)
    {
        var salons = await _service.GetSalonsAsync(search, isActive, ct);
        return Ok(ApiResponse<IReadOnlyList<PlatformSalonDto>>.CreateSuccess(salons, "Platform salons retrieved successfully."));
    }

    [HttpPost("salons")]
    [ProducesResponseType(typeof(ApiResponse<PlatformSalonDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSalon(
        [FromBody] CreatePlatformSalonRequest request,
        CancellationToken ct)
    {
        var salon = await _service.CreateSalonAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<PlatformSalonDto>.CreateSuccess(salon, "Salon created successfully."));
    }

    [HttpPut("salons/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PlatformSalonDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSalon(
        Guid id,
        [FromBody] UpdatePlatformSalonRequest request,
        CancellationToken ct)
    {
        var updated = await _service.UpdateSalonAsync(id, request, ct);
        return Ok(ApiResponse<PlatformSalonDto>.CreateSuccess(updated, "Salon updated successfully."));
    }

    [HttpPatch("salons/{id:guid}/status")]
    [ProducesResponseType(typeof(ApiResponse<PlatformSalonDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ToggleSalonStatus(
        Guid id,
        [FromBody] ToggleStatusRequest request,
        CancellationToken ct)
    {
        var updated = await _service.ToggleSalonStatusAsync(id, request.IsActive, ct);
        return Ok(ApiResponse<PlatformSalonDto>.CreateSuccess(updated, "Salon status updated successfully."));
    }

    [HttpGet("active-monitoring")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ActiveSessionUserDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveMonitoring(
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
    {
        var active = await _service.GetActiveMonitoringAsync(limit, ct);
        return Ok(ApiResponse<IReadOnlyList<ActiveSessionUserDto>>.CreateSuccess(active, "Active monitoring data retrieved successfully."));
    }
}

public record ToggleStatusRequest(bool IsActive);
