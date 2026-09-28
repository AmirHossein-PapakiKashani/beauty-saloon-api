namespace BarberSalon.Application.Admin.DTOs;

public sealed record TimeGapDto(
    string Id,
    string Date,
    string DayLabel,
    string Time,
    Guid StaffId,
    string StaffName,
    int DurationMinutes);
