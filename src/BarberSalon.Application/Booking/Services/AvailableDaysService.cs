using BarberSalon.Application.Booking.DTOs;
using BarberSalon.Application.Common.Exceptions;
using BarberSalon.Application.Common.Interfaces;

namespace BarberSalon.Application.Booking.Services;

/// <summary>
/// Computes the list of upcoming days that customers can book.
/// </summary>
public sealed class AvailableDaysService
{
    /// <summary>Default number of days returned when no count is supplied.</summary>
    public const int DefaultCount = 7;

    /// <summary>Maximum number of days that can be requested in a single call.</summary>
    public const int MaxCount = 30;

    // Indexed by System.DayOfWeek (Sunday = 0 ... Saturday = 6),
    // mirroring the Persian weekday labels used by the frontend.
    private static readonly string[] PersianDayNames =
    {
        "یکشنبه", "دوشنبه", "سهشنبه", "چهارشنبه", "پنجشنبه", "جمعه", "شنبه"
    };

    private readonly IClock _clock;

    /// <summary>Creates a service using the given clock as the "today" reference.</summary>
    /// <param name="clock">The clock used to determine the current date.</param>
    public AvailableDaysService(IClock clock)
    {
        _clock = clock;
    }

    /// <summary>
    /// Returns the upcoming <paramref name="count"/> days, starting from today.
    /// Days are ordered ascending and are consecutive.
    /// </summary>
    /// <param name="count">Number of days to return. Must be between 1 and <see cref="MaxCount"/>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of <see cref="AvailableDayDto"/> ordered ascending by date.</returns>
    /// <exception cref="ValidationException">Thrown when <paramref name="count"/> is out of range.</exception>
    public async Task<List<AvailableDayDto>> GetAvailableDaysAsync(
        int count = DefaultCount,
        CancellationToken cancellationToken = default)
    {
        if (count < 1 || count > MaxCount)
        {
            throw new ValidationException($"count must be between 1 and {MaxCount}.");
        }

        var today = DateOnly.FromDateTime(_clock.UtcNow);
        var days = new List<AvailableDayDto>(count);

        for (var i = 0; i < count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var day = today.AddDays(i);
            days.Add(new AvailableDayDto(
                day.Day.ToString(),
                PersianDayNames[(int)day.DayOfWeek],
                day.ToString("yyyy-MM-dd")));
        }

        return await Task.FromResult(days);
    }
}