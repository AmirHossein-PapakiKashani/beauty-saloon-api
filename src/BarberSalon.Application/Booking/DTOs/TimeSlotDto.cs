namespace BarberSalon.Application.Booking.DTOs;

/// <summary>
/// Read model representing a bookable time slot and its availability status.
/// Matches the frontend TimeSlot contract.
/// </summary>
/// <param name="Time">The time formatted as HH:mm (e.g. "09:00", "14:30").</param>
/// <param name="Available">Whether the time slot is free for booking.</param>
public sealed record TimeSlotDto(string Time, bool Available);
