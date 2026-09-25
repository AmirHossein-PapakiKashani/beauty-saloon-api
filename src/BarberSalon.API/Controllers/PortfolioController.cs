using BarberSalon.API.Common;
using BarberSalon.Application.Portfolio.DTOs;
using BarberSalon.Application.Portfolio.Services;
using Microsoft.AspNetCore.Mvc;

namespace BarberSalon.API.Controllers;

[ApiController]
[Route("api/v1/portfolio")]
public sealed class PortfolioController(PortfolioService portfolioService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<PortfolioItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] string? category, [FromQuery] Guid? staffId, CancellationToken ct)
    {
        var items = await portfolioService.GetAllAsync(category, staffId, ct);
        return Ok(ApiResponse<List<PortfolioItemDto>>.CreateSuccess(items, "Portfolio retrieved successfully."));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PortfolioItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var item = await portfolioService.GetByIdAsync(id, ct);
        if (item is null)
            return NotFound(new ApiResponse<object?>(null, false, $"Portfolio item '{id}' not found."));

        return Ok(ApiResponse<PortfolioItemDto>.CreateSuccess(item, "Portfolio item retrieved successfully."));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PortfolioItemDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreatePortfolioItemRequest req, CancellationToken ct)
    {
        var item = await portfolioService.CreateAsync(req, ct);
        return Created($"/api/v1/portfolio/{item.Id}",
            ApiResponse<PortfolioItemDto>.CreateSuccess(item, "Portfolio item created successfully."));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PortfolioItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePortfolioItemRequest req, CancellationToken ct)
    {
        var item = await portfolioService.UpdateAsync(id, req, ct);
        if (item is null)
            return NotFound(new ApiResponse<object?>(null, false, $"Portfolio item '{id}' not found."));

        return Ok(ApiResponse<PortfolioItemDto>.CreateSuccess(item, "Portfolio item updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await portfolioService.DeleteAsync(id, ct);
        if (!deleted)
            return NotFound(new ApiResponse<object?>(null, false, $"Portfolio item '{id}' not found."));

        return Ok(ApiResponse<object?>.CreateSuccess(new { id }, "Portfolio item deleted successfully."));
    }

    [HttpPost("{id:guid}/like")]
    [ProducesResponseType(typeof(ApiResponse<PortfolioItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Like(Guid id, CancellationToken ct)
    {
        var item = await portfolioService.LikeAsync(id, ct);
        if (item is null)
            return NotFound(new ApiResponse<object?>(null, false, $"Portfolio item '{id}' not found."));

        return Ok(ApiResponse<PortfolioItemDto>.CreateSuccess(item, "Portfolio item liked successfully."));
    }

    [HttpPatch("{id:guid}/toggle-featured")]
    [ProducesResponseType(typeof(ApiResponse<PortfolioItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ToggleFeatured(Guid id, CancellationToken ct)
    {
        var item = await portfolioService.ToggleFeaturedAsync(id, ct);
        if (item is null)
            return NotFound(new ApiResponse<object?>(null, false, $"Portfolio item '{id}' not found."));

        return Ok(ApiResponse<PortfolioItemDto>.CreateSuccess(item, "Portfolio item featured status toggled."));
    }

    [HttpPatch("{id:guid}/toggle-publish")]
    [HttpPatch("{id:guid}/toggle-published")]
    [ProducesResponseType(typeof(ApiResponse<PortfolioItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> TogglePublished(Guid id, CancellationToken ct)
    {
        var item = await portfolioService.TogglePublishedAsync(id, ct);
        if (item is null)
            return NotFound(new ApiResponse<object?>(null, false, $"Portfolio item '{id}' not found."));

        return Ok(ApiResponse<PortfolioItemDto>.CreateSuccess(item, "Portfolio item publish status toggled."));
    }
}
