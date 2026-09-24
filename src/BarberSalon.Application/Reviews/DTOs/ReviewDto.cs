namespace BarberSalon.Application.Reviews.DTOs;

public sealed record ReviewDto(
    Guid Id,
    Guid? AppointmentId,
    Guid CustomerId,
    string CustomerName,
    Guid? StaffId,
    string StaffName,
    Guid? ServiceId,
    string ServiceName,
    int Rating,
    string Comment,
    string Status,
    string CreatedAt,
    string? UpdatedAt);
