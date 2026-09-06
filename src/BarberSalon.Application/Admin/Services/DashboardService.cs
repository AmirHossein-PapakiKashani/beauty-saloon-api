using BarberSalon.Application.Admin.DTOs;
using BarberSalon.Application.Booking.Interfaces;
using BarberSalon.Application.Common.Interfaces;
using BarberSalon.Domain.Booking.Enums;

namespace BarberSalon.Application.Admin.Services;

/// <summary>
/// Application service providing aggregated statistics and operational metrics for the salon administrator dashboard.
/// </summary>
public sealed class DashboardService
{
    private const int WeeklyRollingDays = 7;
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IClock _clock;

    /// <summary>
    /// Initializes a new instance of <see cref="DashboardService"/>.
    /// </summary>
    /// <param name="appointmentRepository">Repository for querying appointment records.</param>
    /// <param name="clock">Time provider abstraction for deterministic calculations.</param>
    public DashboardService(
        IAppointmentRepository appointmentRepository,
        IClock clock)
    {
        _appointmentRepository = appointmentRepository;
        _clock = clock;
    }

    /// <summary>
    /// Computes and returns aggregated business and operational metrics for the dashboard.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="DashboardStatsDto"/> containing today's stats, pending appointments, weekly customer count, and today's revenue.</returns>
    public async Task<DashboardStatsDto> GetDashboardStatsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var today = DateOnly.FromDateTime(_clock.UtcNow);
        var weekStart = today.AddDays(-(WeeklyRollingDays - 1));

        var allAppointments = await _appointmentRepository.GetAllAsync(cancellationToken);

        var todayAppointments = allAppointments
            .Count(a => a.TimeSlot.Date == today &&
                        a.Status != AppointmentStatus.Cancelled &&
                        a.Status != AppointmentStatus.NoShow);

        var pendingAppointments = allAppointments
            .Count(a => a.Status == AppointmentStatus.Pending);

        var weeklyCustomers = allAppointments
            .Where(a => a.TimeSlot.Date >= weekStart &&
                        a.TimeSlot.Date <= today &&
                        a.Status != AppointmentStatus.Cancelled &&
                        a.Status != AppointmentStatus.NoShow)
            .Select(a => a.CustomerId)
            .Distinct()
            .Count();

        var todayRevenue = allAppointments
            .Where(a => a.TimeSlot.Date == today &&
                        (a.Status == AppointmentStatus.Confirmed || a.Status == AppointmentStatus.Completed))
            .Sum(a => a.Price);

        return new DashboardStatsDto(
            TodayAppointments: todayAppointments,
            PendingAppointments: pendingAppointments,
            WeeklyCustomers: weeklyCustomers,
            TodayRevenue: todayRevenue);
    }
}
