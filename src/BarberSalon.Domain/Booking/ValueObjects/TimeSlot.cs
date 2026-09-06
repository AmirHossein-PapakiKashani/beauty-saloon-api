using BarberSalon.Domain.Common;

namespace BarberSalon.Domain.Booking.ValueObjects;

/// <summary>
/// Immutable value object representing a specific block of time on a given date.
/// </summary>
public sealed class TimeSlot : IEquatable<TimeSlot>
{
    /// <summary>The calendar date for this time slot.</summary>
    public DateOnly Date { get; private set; }

    /// <summary>The start time of the slot.</summary>
    public TimeOnly StartTime { get; private set; }

    /// <summary>The end time of the slot.</summary>
    public TimeOnly EndTime { get; private set; }

    // Private constructor for EF Core
    private TimeSlot() { }

    private TimeSlot(DateOnly date, TimeOnly startTime, TimeOnly endTime)
    {
        Date = date;
        StartTime = startTime;
        EndTime = endTime;
    }

    /// <summary>
    /// Creates a new immutable <see cref="TimeSlot"/> instance.
    /// </summary>
    /// <param name="date">Calendar date.</param>
    /// <param name="startTime">Slot start time.</param>
    /// <param name="endTime">Slot end time (must be strictly after start time).</param>
    /// <returns>A valid <see cref="TimeSlot"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when end time is not after start time.</exception>
    public static TimeSlot Create(DateOnly date, TimeOnly startTime, TimeOnly endTime)
    {
        if (endTime <= startTime)
        {
            throw new DomainException("TimeSlot end time must be strictly after start time.");
        }

        return new TimeSlot(date, startTime, endTime);
    }

    /// <summary>
    /// Checks if this time slot overlaps with another time slot on the same date.
    /// </summary>
    /// <param name="other">The other time slot to compare.</param>
    /// <returns>True if they overlap, otherwise false.</returns>
    public bool OverlapsWith(TimeSlot other)
    {
        if (other == null || Date != other.Date)
        {
            return false;
        }

        return StartTime < other.EndTime && other.StartTime < EndTime;
    }

    /// <summary>
    /// Checks whether a specific time falls within this slot interval [StartTime, EndTime).
    /// </summary>
    /// <param name="time">The time to check.</param>
    /// <returns>True if time is greater than or equal to StartTime and less than EndTime.</returns>
    public bool Contains(TimeOnly time) => time >= StartTime && time < EndTime;

    /// <inheritdoc />
    public bool Equals(TimeSlot? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Date == other.Date && StartTime == other.StartTime && EndTime == other.EndTime;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as TimeSlot);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Date, StartTime, EndTime);

    /// <inheritdoc />
    public override string ToString() => $"{Date:yyyy-MM-dd} {StartTime:HH:mm}-{EndTime:HH:mm}";
}
