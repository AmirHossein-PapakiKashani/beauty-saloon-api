using BarberSalon.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace BarberSalon.Infrastructure.ExternalServices;

/// <inheritdoc cref="ISmsService"/>
public sealed class SmsService : ISmsService
{
    private readonly ILogger<SmsService>? _logger;

    public SmsService(ILogger<SmsService>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task SendOtpAsync(string phoneNumber, string code, CancellationToken cancellationToken = default)
    {
        // Default local/development SMS mock delivery
        _logger?.LogInformation(">>> [SMS SERVICE MOCK] Sent OTP {Code} to {PhoneNumber} <<<", code, phoneNumber);
        return Task.CompletedTask;
    }
}

