using BarberSalon.API.Common;
using BarberSalon.Application.Common.Exceptions;
using BarberSalon.Application.Customers.DTOs;
using BarberSalon.Application.Customers.Services;
using Microsoft.AspNetCore.Mvc;

namespace BarberSalon.API.Controllers;

/// <summary>
/// Manages customer profiles and CRM records.
/// </summary>
[ApiController]
[Route("api/v1/customers")]
public sealed class CustomersController : ControllerBase
{
    private readonly CustomerService _customerService;

    /// <summary>
    /// Initializes a new instance of <see cref="CustomersController"/>.
    /// </summary>
    /// <param name="customerService">Application service for customers.</param>
    public CustomersController(CustomerService customerService)
    {
        _customerService = customerService;
    }

    /// <summary>
    /// Creates a new customer profile.
    /// </summary>
    /// <param name="request">Customer creation payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created customer wrapped in the standard response envelope.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CustomerDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _customerService.CreateCustomerAsync(request, cancellationToken);
            return CreatedAtAction(
                nameof(GetById),
                new { id = result.Id },
                ApiResponse<CustomerDto>.CreateSuccess(result, "Customer created successfully."));
        }
        catch (ValidationException ex)
        {
            return BadRequest(new ApiResponse<object?>(null, false, ex.Message));
        }
    }

    /// <summary>
    /// Returns customer details by their unique identifier.
    /// </summary>
    /// <param name="id">The customer GUID identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The customer details wrapped in the standard response envelope.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var customer = await _customerService.GetByIdAsync(id, cancellationToken);
            return Ok(ApiResponse<CustomerDto>.CreateSuccess(customer, "Customer retrieved successfully."));
        }
        catch (NotFoundException ex)
        {
            return NotFound(new ApiResponse<object?>(null, false, ex.Message));
        }
    }

    /// <summary>
    /// Updates an existing customer profile.
    /// </summary>
    /// <param name="id">The customer GUID identifier.</param>
    /// <param name="request">Customer update payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated customer wrapped in the standard response envelope.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _customerService.UpdateCustomerAsync(id, request, cancellationToken);
            return Ok(ApiResponse<CustomerDto>.CreateSuccess(result, "Customer updated successfully."));
        }
        catch (ValidationException ex)
        {
            return BadRequest(new ApiResponse<object?>(null, false, ex.Message));
        }
        catch (NotFoundException ex)
        {
            return NotFound(new ApiResponse<object?>(null, false, ex.Message));
        }
    }

    /// <summary>
    /// Searches active customers matching a search query across name and phone number.
    /// </summary>
    /// <param name="query">Search term for full name or phone number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of matching customer profiles wrapped in the standard response envelope.</returns>
    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<List<CustomerDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] string? query = null,
        CancellationToken cancellationToken = default)
    {
        var customers = await _customerService.SearchCustomersAsync(query, cancellationToken);
        return Ok(ApiResponse<List<CustomerDto>>.CreateSuccess(customers, "Customers search completed successfully."));
    }

    /// <summary>
    /// Returns all customers, optionally filtered by status ("active", "inactive", or all by default) or search query.
    /// </summary>
    /// <param name="status">Optional status filter ("active", "inactive", "all").</param>
    /// <param name="query">Optional search query to filter by name or phone number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of customer profiles wrapped in the standard response envelope.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<CustomerDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status = null,
        [FromQuery] string? query = null,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(query))
        {
            var searchResults = await _customerService.SearchCustomersAsync(query, cancellationToken);
            return Ok(ApiResponse<List<CustomerDto>>.CreateSuccess(searchResults, "Customers search completed successfully."));
        }

        var customers = await _customerService.GetCustomersAsync(status, cancellationToken);
        return Ok(ApiResponse<List<CustomerDto>>.CreateSuccess(customers, "Customers retrieved successfully."));
    }

    /// <summary>
    /// Archives (soft deletes) a customer by unique identifier.
    /// </summary>
    /// <param name="id">The customer unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A standard response envelope.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _customerService.ArchiveCustomerAsync(id, cancellationToken);
            return Ok(ApiResponse<object?>.CreateSuccess(null, "Customer archived successfully."));
        }
        catch (NotFoundException ex)
        {
            return NotFound(new ApiResponse<object?>(null, false, ex.Message));
        }
    }
}

