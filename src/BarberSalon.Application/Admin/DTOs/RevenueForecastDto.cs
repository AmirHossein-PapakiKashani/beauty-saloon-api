namespace BarberSalon.Application.Admin.DTOs;

public sealed record RevenueForecastDto(
    string WeekLabel,
    decimal ConfirmedRevenue,
    decimal PendingRevenue,
    decimal TotalExpected,
    int ConfirmedCount,
    int PendingCount,
    int CancelledCount,
    List<DailyRevenueBreakdownDto> DailyBreakdown);
