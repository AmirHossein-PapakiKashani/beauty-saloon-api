namespace BarberSalon.Application.Booking.DTOs;

/// <summary>
/// Request payload to update an appointment's lifecycle status.
/// </summary>
/// <param name="Status">The new status (confirmed, completed, cancelled, no_show).</param>
/// <param name="Reason">Optional reason for cancellation.</param>
public sealed record UpdateAppointmentStatusRequest(string Status, string? Reason = null);
