namespace BarberSalon.Application.Auth.Interfaces;

/// <summary>
/// Abstraction for generating numeric one-time password codes.
/// </summary>
public interface IOtpGenerator
{
    /// <summary>
    /// Generates a 5-digit numeric OTP code.
    /// </summary>
    /// <returns>A string containing exactly 5 numeric digits.</returns>
    string Generate();
}
