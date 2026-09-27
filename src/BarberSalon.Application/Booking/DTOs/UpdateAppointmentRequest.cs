namespace BarberSalon.Application.Booking.DTOs;

/// <summary>
/// Request payload for updating/rescheduling an existing appointment.
/// </summary>
/// <param name="StaffId">New or existing staff member ID.</param>
/// <param name="SalonServiceId">New or existing salon service ID.</param>
/// <param name="Date">Appointment date (yyyy-MM-dd).</param>
/// <param name="StartTime">Appointment start time (HH:mm).</param>
/// <param name="Price">Optional updated price. If null, current or service default is used.</param>
/// <param name="Notes">Optional special notes.</param>
public sealed record UpdateAppointmentRequest(
    Guid StaffId,
    Guid SalonServiceId,
    string Date,
    string StartTime,
    decimal? Price = null,
    string? Notes = null
);
