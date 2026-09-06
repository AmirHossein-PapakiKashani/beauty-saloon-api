using BarberSalon.API.Common;
using BarberSalon.Application.Auth.DTOs;
using BarberSalon.Application.Auth.Services;
using BarberSalon.Application.Common.Exceptions;
using BarberSalon.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BarberSalon.API.Controllers;

/// <summary>
/// Manages user authentication and OTP operations.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    /// <summary>
    /// Initializes a new instance of <see cref="AuthController"/>.
    /// </summary>
    /// <param name="authService">Application service for authentication operations.</param>
    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Sends a one-time password (OTP) verification code to the given phone number.
    /// </summary>
    /// <remarks>
    /// Generates a 5-digit verification code valid for 5 minutes.
    /// Invalidates any previous unexpired OTP codes for the same phone number.
    /// </remarks>
    /// <param name="request">Request containing the recipient mobile phone number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Status of OTP dispatch wrapped in standard response envelope.</returns>
    [HttpPost("send-otp")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<SendOtpResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendOtp(
        [FromBody] SendOtpRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _authService.SendOtpAsync(request, cancellationToken);
            return Ok(ApiResponse<SendOtpResponse>.CreateSuccess(result, "کد تایید با موفقیت ارسال شد."));
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

    /// <summary>
    /// Verifies the OTP code for the given phone number and returns the authenticated user.
    /// </summary>
    /// <remarks>
    /// Verifies 5-digit verification code against the active record.
    /// If valid, consumes the code and creates a new customer profile if first-time login.
    /// </remarks>
    /// <param name="request">Request containing recipient mobile phone number and 5-digit OTP code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authentication result and user profile wrapped in standard response envelope.</returns>
    [HttpPost("verify-otp")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<VerifyOtpResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyOtp(
        [FromBody] VerifyOtpRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _authService.VerifyOtpAsync(request, cancellationToken);
            return Ok(ApiResponse<VerifyOtpResponse>.CreateSuccess(result, "ورود با موفقیت انجام شد."));
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
