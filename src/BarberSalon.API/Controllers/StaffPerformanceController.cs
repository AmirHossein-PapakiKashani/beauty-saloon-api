using BarberSalon.API.Common;
using BarberSalon.Application.StaffPerformance.DTOs;
using BarberSalon.Application.StaffPerformance.Services;
using Microsoft.AspNetCore.Mvc;

namespace BarberSalon.API.Controllers;

[ApiController]
[Route("api/v1/staff-performance")]
public sealed class StaffPerformanceController(StaffPerformanceService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<StaffPerformanceDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Get([FromQuery] string? from, [FromQuery] string? to, CancellationToken ct)
    {
        DateOnly? fromDate = null;
        if (!string.IsNullOrWhiteSpace(from))
        {
            if (!DateOnly.TryParse(from, out var parsedFrom))
            {
                return BadRequest(new ApiResponse<object?>(null, false, "Invalid 'from' date format. Expected yyyy-MM-dd."));
            }
            fromDate = parsedFrom;
        }

        DateOnly? toDate = null;
        if (!string.IsNullOrWhiteSpace(to))
        {
            if (!DateOnly.TryParse(to, out var parsedTo))
            {
                return BadRequest(new ApiResponse<object?>(null, false, "Invalid 'to' date format. Expected yyyy-MM-dd."));
            }
            toDate = parsedTo;
        }

        if (fromDate.HasValue && toDate.HasValue && fromDate.Value > toDate.Value)
        {
            return BadRequest(new ApiResponse<object?>(null, false, "'from' date cannot be after 'to' date."));
        }

        var result = await service.GetStaffPerformanceAsync(from, to, ct);
        return Ok(ApiResponse<List<StaffPerformanceDto>>.CreateSuccess(result, "Staff performance metrics retrieved successfully."));
    }
}
