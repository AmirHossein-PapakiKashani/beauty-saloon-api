using BarberSalon.Application.Common.Interfaces;

namespace BarberSalon.Infrastructure.Time;

/// <summary>
/// Production implementation of <see cref="IClock"/> that returns the real system clock.
/// </summary>
public sealed class SystemClock : IClock
{
    /// <inheritdoc />
    public DateTime UtcNow => DateTime.UtcNow;
}