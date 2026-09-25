namespace BarberSalon.Application.BeautyProfile.DTOs;

public sealed record BeautyHistoryEntryDto(
    Guid Id,
    Guid CustomerId,
    string ServiceName,
    string StaffName,
    string Date,
    string Formula,
    string Notes,
    string? PhotoUrl,
    DateTime CreatedAt
);
