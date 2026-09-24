using BarberSalon.API.Common;
using BarberSalon.Application.BeautyProfile.DTOs;
using BarberSalon.Application.BeautyProfile.Services;
using BarberSalon.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace BarberSalon.API.Controllers;

[ApiController]
[Route("api/v1/beauty-profile")]
public sealed class BeautyProfileController(BeautyProfileService service) : ControllerBase
{
    [HttpGet("{customerId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<BeautyProfileDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get([FromRoute] Guid customerId, CancellationToken ct)
    {
        var result = await service.GetByCustomerIdAsync(customerId, ct);
        return Ok(ApiResponse<BeautyProfileDto>.CreateSuccess(result, "Beauty profile retrieved successfully."));
    }

    [HttpPut("{customerId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<BeautyProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upsert([FromRoute] Guid customerId, [FromBody] UpdateBeautyProfileRequest request, CancellationToken ct)
    {
        try
        {
            var result = await service.UpsertAsync(customerId, request, ct);
            return Ok(ApiResponse<BeautyProfileDto>.CreateSuccess(result, "Beauty profile updated successfully."));
        }
        catch (ValidationException ex)
        {
            return BadRequest(new ApiResponse<object?>(null, false, ex.Message));
        }
    }
}
