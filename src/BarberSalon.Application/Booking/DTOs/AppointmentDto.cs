namespace BarberSalon.Application.Booking.DTOs;

/// <summary>
/// Read model returned to the client representing an appointment.
/// </summary>
public sealed record AppointmentDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    Guid StaffId,
    string StaffName,
    Guid ServiceId,
    string ServiceName,
    string Date,
    string Time,
    string Status,
    decimal Price,
    string Notes,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
