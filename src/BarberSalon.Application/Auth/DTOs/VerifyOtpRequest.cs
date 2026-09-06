namespace BarberSalon.Application.Auth.DTOs;

/// <summary>
/// Request payload containing phone number and 5-digit verification code.
/// </summary>
/// <param name="PhoneNumber">The recipient mobile phone number.</param>
/// <param name="Code">The 5-digit OTP verification code.</param>
public sealed record VerifyOtpRequest(string PhoneNumber, string Code);
