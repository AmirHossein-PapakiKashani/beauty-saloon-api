namespace BarberSalon.Application.Waitlist.DTOs;

public sealed record WaitlistEntryDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    string CustomerPhone,
    Guid StaffId,
    string StaffName,
    Guid ServiceId,
    string ServiceName,
    string Date,
    string Time,
    string Status,
    int QueuePosition,
    string? NotifiedAt,
    string CreatedAt,
    string? UpdatedAt);
