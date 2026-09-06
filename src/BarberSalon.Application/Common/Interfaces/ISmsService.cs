namespace BarberSalon.Application.Common.Interfaces;

/// <summary>
/// Abstraction for external SMS gateway providers to send notifications and verification codes.
/// </summary>
public interface ISmsService
{
    /// <summary>
    /// Sends an OTP verification code to the recipient phone number.
    /// </summary>
    /// <param name="phoneNumber">Recipient phone number.</param>
    /// <param name="code">The numeric verification code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendOtpAsync(string phoneNumber, string code, CancellationToken cancellationToken = default);
}
