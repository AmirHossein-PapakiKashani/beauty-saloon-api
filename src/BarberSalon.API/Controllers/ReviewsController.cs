using BarberSalon.API.Common;
using BarberSalon.Application.Reviews.DTOs;
using BarberSalon.Application.Reviews.Services;
using Microsoft.AspNetCore.Mvc;

namespace BarberSalon.API.Controllers;

[ApiController]
[Route("api/v1/reviews")]
public sealed class ReviewsController(ReviewService reviewService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<ReviewDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? customerId,
        [FromQuery] Guid? staffId,
        [FromQuery] Guid? serviceId,
        CancellationToken ct)
    {
        var list = await reviewService.GetAllAsync(customerId, staffId, serviceId, ct);
        return Ok(ApiResponse<List<ReviewDto>>.CreateSuccess(list, "Reviews retrieved successfully."));
    }

    [HttpGet("staff/{staffId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<List<ReviewDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByStaff(Guid staffId, CancellationToken ct)
    {
        var list = await reviewService.GetByStaffIdAsync(staffId, ct);
        return Ok(ApiResponse<List<ReviewDto>>.CreateSuccess(list, "Staff reviews retrieved successfully."));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ReviewDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromBody] CreateReviewRequest req,
        CancellationToken ct)
    {
        var review = await reviewService.CreateAsync(req, ct);
        return Created($"/api/v1/reviews/{review.Id}",
            ApiResponse<ReviewDto>.CreateSuccess(review, "Review created successfully."));
    }

    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(typeof(ApiResponse<ReviewDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateReviewStatusRequest req,
        CancellationToken ct)
    {
        var updated = await reviewService.UpdateStatusAsync(id, req.Status, ct);
        if (updated is null)
            return NotFound(new ApiResponse<object?>(null, false, $"Review with ID '{id}' not found."));

        return Ok(ApiResponse<ReviewDto>.CreateSuccess(updated, "Review status updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await reviewService.DeleteAsync(id, ct);
        if (!deleted)
            return NotFound(new ApiResponse<object?>(null, false, $"Review with ID '{id}' not found."));

        return Ok(ApiResponse<object?>.CreateSuccess(new { id }, "Review deleted successfully."));
    }
}
