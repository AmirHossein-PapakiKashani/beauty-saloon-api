namespace BarberSalon.Application.Admin.DTOs;

public sealed record DailyRevenueBreakdownDto(string Date, string DayLabel, decimal Amount);
