namespace BarberSalon.Application.Reviews.DTOs;

public sealed record CreateReviewRequest(
    Guid CustomerId,
    string? CustomerName,
    int Rating,
    string Comment,
    Guid? StaffId = null,
    string? StaffName = null,
    Guid? ServiceId = null,
    string? ServiceName = null,
    Guid? AppointmentId = null);
