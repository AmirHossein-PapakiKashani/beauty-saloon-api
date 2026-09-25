namespace BarberSalon.Application.BeautyProfile.DTOs;

public sealed record CreateBeautyHistoryEntryRequest(
    string ServiceName,
    string StaffName,
    string Date,
    string Formula,
    string Notes,
    string? PhotoUrl
);
