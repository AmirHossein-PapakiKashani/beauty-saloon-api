using BarberSalon.API.Common;
using BarberSalon.Application.Waitlist.DTOs;
using BarberSalon.Application.Waitlist.Services;
using Microsoft.AspNetCore.Mvc;

namespace BarberSalon.API.Controllers;

[ApiController]
[Route("api/v1/waitlist")]
public sealed class WaitlistController(WaitlistService waitlistService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<WaitlistEntryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] Guid? staffId,
        [FromQuery] string? date,
        CancellationToken ct)
    {
        var list = await waitlistService.GetAllAsync(status, staffId, date, ct);
        return Ok(ApiResponse<List<WaitlistEntryDto>>.CreateSuccess(list, "Waitlist entries retrieved successfully."));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<WaitlistEntryDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Join(
        [FromBody] JoinWaitlistRequest req,
        CancellationToken ct)
    {
        var entry = await waitlistService.JoinAsync(req, ct);
        return Created($"/api/v1/waitlist/{entry.Id}",
            ApiResponse<WaitlistEntryDto>.CreateSuccess(entry, "Joined waitlist successfully."));
    }

    [HttpPost("{id:guid}/notify")]
    [ProducesResponseType(typeof(ApiResponse<WaitlistEntryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Notify(
        Guid id,
        [FromBody] NotifyWaitlistRequest? req,
        CancellationToken ct)
    {
        var notified = await waitlistService.NotifyAsync(id, ct);
        if (notified is null)
            return NotFound(new ApiResponse<object?>(null, false, $"Waitlist entry with ID '{id}' not found."));

        return Ok(ApiResponse<WaitlistEntryDto>.CreateSuccess(notified, "Customer notified successfully."));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var cancelled = await waitlistService.CancelAsync(id, ct);
        if (!cancelled)
            return NotFound(new ApiResponse<object?>(null, false, $"Waitlist entry with ID '{id}' not found."));

        return Ok(ApiResponse<object?>.CreateSuccess(new { id }, "Waitlist entry cancelled successfully."));
    }
}
