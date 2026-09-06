namespace BarberSalon.Application.Booking.DTOs;

/// <summary>
/// Represents a single day that can be booked.
/// </summary>
/// <param name="Date">Day of the month as a string (e.g. "15").</param>
/// <param name="DayName">Localized weekday name (Persian, e.g. "سه‌شنبه").</param>
/// <param name="FullDate">ISO-8601 date (yyyy-MM-dd).</param>
public sealed record AvailableDayDto(string Date, string DayName, string FullDate);