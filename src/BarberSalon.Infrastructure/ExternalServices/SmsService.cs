using BarberSalon.Application.Common.Interfaces;

namespace BarberSalon.Infrastructure.ExternalServices;

/// <inheritdoc cref="ISmsService"/>
public sealed class SmsService : ISmsService
{
    /// <inheritdoc/>
    public Task SendOtpAsync(string phoneNumber, string code, CancellationToken cancellationToken = default)
    {
        // Default local/development SMS mock delivery
        return Task.CompletedTask;
    }
}
