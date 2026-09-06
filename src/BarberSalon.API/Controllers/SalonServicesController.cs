using BarberSalon.API.Common;
using BarberSalon.Application.SalonServices.DTOs;
using BarberSalon.Application.SalonServices.Services;
using Microsoft.AspNetCore.Mvc;

namespace BarberSalon.API.Controllers;

/// <summary>
/// Manages salon services (e.g. Haircut, Beard Trim).
/// </summary>
[ApiController]
[Route("api/v1/salon-services")]
public sealed class SalonServicesController : ControllerBase
{
    private readonly SalonServiceManager _manager;

    /// <summary>Creates a new instance of <see cref="SalonServicesController"/>.</summary>
    /// <param name="manager">Application service for salon services.</param>
    public SalonServicesController(SalonServiceManager manager)
    {
        _manager = manager;
    }

    /// <summary>Returns all active salon services.</summary>
    /// <remarks>Public endpoint — returns active services ordered by category and name.</remarks>
    /// <param name="status">Optional filter by status (e.g. "active"). Defaults to active services.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of active salon services wrapped in standard response envelope.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<SalonServiceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        var services = await _manager.GetAllActiveAsync(cancellationToken);
        return Ok(ApiResponse<List<SalonServiceDto>>.CreateSuccess(services, "Active salon services retrieved successfully."));
    }

    /// <summary>Returns a single salon service by its identifier.</summary>
    /// <param name="id">The unique identifier of the salon service.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The salon service details wrapped in standard response envelope.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<SalonServiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var service = await _manager.GetByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<SalonServiceDto>.CreateSuccess(service, "Salon service retrieved successfully."));
    }
}
