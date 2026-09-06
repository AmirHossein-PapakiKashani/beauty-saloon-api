using BarberSalon.Domain.Auth.Entities;

namespace BarberSalon.Application.Auth.Interfaces;

/// <summary>
/// Data access contract for <see cref="OtpCode"/> entities.
/// </summary>
public interface IOtpCodeRepository
{
    /// <summary>
    /// Returns the latest active (unused and unexpired) OTP code for the given phone number.
    /// </summary>
    /// <param name="phoneNumber">The recipient phone number.</param>
    /// <param name="currentUtc">The current UTC timestamp.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The active <see cref="OtpCode"/> or null if none found.</returns>
    Task<OtpCode?> GetActiveByPhoneNumberAsync(string phoneNumber, DateTime currentUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all active (unused and unexpired) OTP codes for the given phone number.
    /// </summary>
    /// <param name="phoneNumber">The recipient phone number.</param>
    /// <param name="currentUtc">The current UTC timestamp.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of active <see cref="OtpCode"/> records.</returns>
    Task<List<OtpCode>> GetActiveListByPhoneNumberAsync(string phoneNumber, DateTime currentUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new OTP code to the EF Core change tracker.
    /// </summary>
    /// <param name="otpCode">The OTP code entity to persist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(OtpCode otpCode, CancellationToken cancellationToken = default);
}
