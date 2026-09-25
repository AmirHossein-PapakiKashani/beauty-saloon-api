using BarberSalon.Application.Admin.DTOs;
using BarberSalon.Application.Booking.Interfaces;
using BarberSalon.Application.Common.Interfaces;
using BarberSalon.Application.Customers.Interfaces;
using BarberSalon.Application.SalonServices.Interfaces;
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
    private readonly ICustomerRepository? _customerRepository;
    private readonly ISalonServiceRepository? _salonServiceRepository;

    /// <summary>
    /// Initializes a new instance of <see cref="DashboardService"/>.
    /// </summary>
    /// <param name="appointmentRepository">Repository for querying appointment records.</param>
    /// <param name="clock">Time provider abstraction for deterministic calculations.</param>
    /// <param name="customerRepository">Repository for querying customer profiles.</param>
    /// <param name="salonServiceRepository">Repository for querying salon services.</param>
    public DashboardService(
        IAppointmentRepository appointmentRepository,
        IClock clock,
        ICustomerRepository? customerRepository = null,
        ISalonServiceRepository? salonServiceRepository = null)
    {
        _appointmentRepository = appointmentRepository;
        _clock = clock;
        _customerRepository = customerRepository;
        _salonServiceRepository = salonServiceRepository;
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

    /// <summary>
    /// Computes and returns recent activity entries for the dashboard feed.
    /// </summary>
    public async Task<List<ActivityLogDto>> GetActivitiesAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var allAppointments = await _appointmentRepository.GetAllAsync(cancellationToken);
        if (allAppointments.Count == 0)
        {
            return [];
        }

        var customers = _customerRepository is not null
            ? (await _customerRepository.GetAllAsync(cancellationToken)).ToDictionary(c => c.Id)
            : new Dictionary<Guid, Domain.Customers.Entities.Customer>();

        var services = _salonServiceRepository is not null
            ? (await _salonServiceRepository.GetAllAsync(cancellationToken)).ToDictionary(s => s.Id)
            : new Dictionary<Guid, Domain.SalonServices.Entities.SalonService>();

        var sortedAppointments = allAppointments
            .OrderByDescending(a => a.CreatedAt)
            .Take(count)
            .ToList();

        var activities = new List<ActivityLogDto>();

        foreach (var appt in sortedAppointments)
        {
            var customerName = customers.TryGetValue(appt.CustomerId, out var c) ? c.FullName : "مشتری";
            var serviceName = services.TryGetValue(appt.SalonServiceId, out var s) ? s.Name : "خدمت";

            string type;
            string description;

            switch (appt.Status)
            {
                case AppointmentStatus.Pending:
                    type = "new";
                    description = $"نوبت جدید: {customerName} - {serviceName}";
                    break;
                case AppointmentStatus.Confirmed:
                case AppointmentStatus.Completed:
                    type = "confirmed";
                    description = $"تأیید نوبت: {customerName} - {serviceName}";
                    break;
                case AppointmentStatus.Cancelled:
                case AppointmentStatus.NoShow:
                    type = "cancelled";
                    description = $"لغو نوبت: {customerName} - {serviceName}";
                    break;
                default:
                    type = "new";
                    description = $"فعالیت نوبت: {customerName} - {serviceName}";
                    break;
            }

            var timeStr = appt.TimeSlot.StartTime.ToString("HH:mm");

            activities.Add(new ActivityLogDto(
                Id: appt.Id.ToString(),
                Type: type,
                Description: description,
                Time: timeStr
            ));
        }

        return activities;
    }
}
