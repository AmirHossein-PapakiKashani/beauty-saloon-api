namespace BarberSalon.Application.Booking.DTOs;

/// <summary>
/// Request payload to record a delay for an appointment.
/// </summary>
/// <param name="Minutes">Delay duration in minutes (typically 10, 15, 20, 30).</param>
public sealed record RecordDelayRequest(int Minutes);
