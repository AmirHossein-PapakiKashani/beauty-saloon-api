namespace BarberSalon.Application.Auth.DTOs;

/// <summary>
/// Request model for dispatching a phone OTP verification code.
/// </summary>
/// <param name="PhoneNumber">The recipient mobile phone number.</param>
public sealed record SendOtpRequest(string PhoneNumber);
