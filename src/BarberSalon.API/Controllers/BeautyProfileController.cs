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

    [HttpGet("{customerId:guid}/history")]
    [ProducesResponseType(typeof(ApiResponse<List<BeautyHistoryEntryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory([FromRoute] Guid customerId, CancellationToken ct)
    {
        var result = await service.GetHistoryAsync(customerId, ct);
        return Ok(ApiResponse<List<BeautyHistoryEntryDto>>.CreateSuccess(result, "Beauty history entries retrieved successfully."));
    }

    [HttpPost("{customerId:guid}/history")]
    [ProducesResponseType(typeof(ApiResponse<BeautyHistoryEntryDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddHistoryEntry(
        [FromRoute] Guid customerId,
        [FromBody] CreateBeautyHistoryEntryRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await service.AddHistoryEntryAsync(customerId, request, ct);
            return StatusCode(StatusCodes.Status201Created, ApiResponse<BeautyHistoryEntryDto>.CreateSuccess(result, "Beauty history entry created successfully."));
        }
        catch (ValidationException ex)
        {
            return BadRequest(new ApiResponse<object?>(null, false, ex.Message));
        }
    }

    [HttpDelete("history/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteHistoryEntry([FromRoute] Guid id, CancellationToken ct)
    {
        var deleted = await service.DeleteHistoryEntryAsync(id, ct);
        if (!deleted)
            return NotFound(new ApiResponse<object?>(null, false, $"Beauty history entry '{id}' not found."));

        return Ok(ApiResponse<object?>.CreateSuccess(new { id }, "Beauty history entry deleted successfully."));
    }
}
