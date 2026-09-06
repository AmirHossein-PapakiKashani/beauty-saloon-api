namespace BarberSalon.Application.Booking.DTOs;

/// <summary>
/// Input model for creating a new appointment.
/// </summary>
public sealed record CreateAppointmentRequest(
    Guid CustomerId,
    Guid StaffId,
    Guid ServiceId,
    string Date,
    string Time,
    string? Notes = null
);
