using BarberSalon.API.Common;
using BarberSalon.Application.Auth.DTOs;
using BarberSalon.Application.Auth.Services;
using BarberSalon.Application.Common.Exceptions;
using BarberSalon.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace BarberSalon.API.Controllers;

/// <summary>
/// Manages user accounts and user profiles.
/// </summary>
[ApiController]
[Route("api/v1/users")]
public sealed class UsersController : ControllerBase
{
    private readonly UserService _userService;

    /// <summary>
    /// Initializes a new instance of <see cref="UsersController"/>.
    /// </summary>
    /// <param name="userService">Application service for user operations.</param>
    public UsersController(UserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Returns user profile details by unique identifier.
    /// </summary>
    /// <param name="id">The unique user GUID identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user profile wrapped in the standard response envelope.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userService.GetByIdAsync(id, cancellationToken);
            return Ok(ApiResponse<UserDto>.CreateSuccess(user, "User profile retrieved successfully."));
        }
        catch (NotFoundException ex)
        {
            return NotFound(new ApiResponse<object?>(null, false, ex.Message));
        }
    }

    /// <summary>
    /// Updates user profile details such as display name.
    /// </summary>
    /// <param name="id">The unique user GUID identifier.</param>
    /// <param name="request">The profile update payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated user profile wrapped in the standard response envelope.</returns>
    [HttpPut("{id:guid}")]
    [HttpPatch("{id:guid}")]
    [HttpPut("{id:guid}/profile")]
    [HttpPost("{id:guid}/profile")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile(
        Guid id,
        [FromBody] UpdateUserProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _userService.UpdateProfileAsync(id, request, cancellationToken);
            return Ok(ApiResponse<UserDto>.CreateSuccess(result, "User profile updated successfully."));
        }
        catch (NotFoundException ex)
        {
            return NotFound(new ApiResponse<object?>(null, false, ex.Message));
        }
        catch (ValidationException ex)
        {
            return BadRequest(new ApiResponse<object?>(null, false, ex.Message));
        }
        catch (DomainException ex)
        {
            return BadRequest(new ApiResponse<object?>(null, false, ex.Message));
        }
    }
}
