namespace BarberSalon.Application.Auth.DTOs;

/// <summary>
/// Response model indicating OTP dispatch outcome and code lifespan.
/// </summary>
/// <param name="Sent">Whether the OTP was dispatched successfully.</param>
/// <param name="ExpiresInSeconds">The remaining lifespan of the code in seconds (default 300).</param>
public sealed record SendOtpResponse(bool Sent, int ExpiresInSeconds);
