namespace BarberSalon.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the current point in time, enabling deterministic time-based use cases in tests.
/// </summary>
public interface IClock
{
    /// <summary>Gets the current UTC date and time.</summary>
    DateTime UtcNow { get; }
}