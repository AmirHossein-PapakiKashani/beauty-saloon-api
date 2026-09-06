namespace BarberSalon.Application.Admin.DTOs;

/// <summary>
/// Aggregated business metrics for the salon administrator dashboard.
/// </summary>
/// <param name="TodayAppointments">Number of appointments scheduled for today (excluding cancelled and no-show).</param>
/// <param name="PendingAppointments">Number of appointments currently awaiting confirmation.</param>
/// <param name="WeeklyCustomers">Number of unique customers who booked appointments within the last 7 days.</param>
/// <param name="TodayRevenue">Total expected or realized revenue for today's confirmed and completed appointments.</param>
public sealed record DashboardStatsDto(
    int TodayAppointments,
    int PendingAppointments,
    int WeeklyCustomers,
    decimal TodayRevenue);
