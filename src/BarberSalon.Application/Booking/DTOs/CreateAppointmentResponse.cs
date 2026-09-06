namespace BarberSalon.Application.Booking.DTOs;

/// <summary>
/// Response payload returned after an appointment is successfully created.
/// </summary>
public sealed record CreateAppointmentResponse(
    AppointmentDto Appointment,
    string BookingCode
);
