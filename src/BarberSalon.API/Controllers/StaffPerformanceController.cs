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
    public async Task<IActionResult> Get([FromQuery] string? from, [FromQuery] string? to, CancellationToken ct)
    {
        var result = await service.GetStaffPerformanceAsync(from, to, ct);
        return Ok(ApiResponse<List<StaffPerformanceDto>>.CreateSuccess(result, "Staff performance metrics retrieved successfully."));
    }
}
