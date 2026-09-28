using BarberSalon.Application.Admin.DTOs;
using BarberSalon.Application.Booking.Interfaces;
using BarberSalon.Application.Common.Interfaces;
using BarberSalon.Application.Customers.Interfaces;
using BarberSalon.Application.SalonServices.Interfaces;
using BarberSalon.Application.Staff.Interfaces;
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
    private readonly IStaffRepository? _staffRepository;

    /// <summary>
    /// Initializes a new instance of <see cref="DashboardService"/>.
    /// </summary>
    /// <param name="appointmentRepository">Repository for querying appointment records.</param>
    /// <param name="clock">Time provider abstraction for deterministic calculations.</param>
    /// <param name="customerRepository">Repository for querying customer profiles.</param>
    /// <param name="salonServiceRepository">Repository for querying salon services.</param>
    /// <param name="staffRepository">Repository for querying staff members.</param>
    public DashboardService(
        IAppointmentRepository appointmentRepository,
        IClock clock,
        ICustomerRepository? customerRepository = null,
        ISalonServiceRepository? salonServiceRepository = null,
        IStaffRepository? staffRepository = null)
    {
        _appointmentRepository = appointmentRepository;
        _clock = clock;
        _customerRepository = customerRepository;
        _salonServiceRepository = salonServiceRepository;
        _staffRepository = staffRepository;
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

    /// <summary>
    /// Returns a revenue forecast for the rolling 7-day window ending today.
    /// </summary>
    public async Task<RevenueForecastDto> GetRevenueForecastAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var today = DateOnly.FromDateTime(_clock.UtcNow);
        var windowStart = today.AddDays(-(WeeklyRollingDays - 1));

        var allAppointments = await _appointmentRepository.GetAllAsync(cancellationToken);

        var windowAppointments = allAppointments
            .Where(a => a.TimeSlot.Date >= windowStart && a.TimeSlot.Date <= today)
            .ToList();

        decimal confirmedRevenue = 0m;
        decimal pendingRevenue = 0m;
        int confirmedCount = 0;
        int pendingCount = 0;
        int cancelledCount = 0;

        foreach (var a in windowAppointments)
        {
            switch (a.Status)
            {
                case AppointmentStatus.Confirmed:
                case AppointmentStatus.Completed:
                    confirmedRevenue += a.Price;
                    confirmedCount++;
                    break;
                case AppointmentStatus.Pending:
                    pendingRevenue += a.Price;
                    pendingCount++;
                    break;
                case AppointmentStatus.Cancelled:
                case AppointmentStatus.NoShow:
                    cancelledCount++;
                    break;
            }
        }

        // Build daily breakdown: one entry per day in the window
        var dailyBreakdown = new List<DailyRevenueBreakdownDto>(WeeklyRollingDays);
        for (var i = 0; i < WeeklyRollingDays; i++)
        {
            var day = windowStart.AddDays(i);
            var amount = windowAppointments
                .Where(a => a.TimeSlot.Date == day &&
                            (a.Status == AppointmentStatus.Confirmed ||
                             a.Status == AppointmentStatus.Completed ||
                             a.Status == AppointmentStatus.Pending))
                .Sum(a => a.Price);

            dailyBreakdown.Add(new DailyRevenueBreakdownDto(
                Date: day.ToString("yyyy-MM-dd"),
                DayLabel: day.DayOfWeek.ToString(),
                Amount: amount));
        }

        var weekLabel = $"{windowStart:yyyy-MM-dd} تا {today:yyyy-MM-dd}";

        return new RevenueForecastDto(
            WeekLabel: weekLabel,
            ConfirmedRevenue: confirmedRevenue,
            PendingRevenue: pendingRevenue,
            TotalExpected: confirmedRevenue + pendingRevenue,
            ConfirmedCount: confirmedCount,
            PendingCount: pendingCount,
            CancelledCount: cancelledCount,
            DailyBreakdown: dailyBreakdown);
    }

    /// <summary>
    /// Returns customers at risk of churning, classified by days since last visit.
    /// </summary>
    public async Task<List<AtRiskCustomerDto>> GetAtRiskCustomersAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_customerRepository is null)
        {
            return [];
        }

        var today = DateOnly.FromDateTime(_clock.UtcNow);
        var customers = await _customerRepository.GetAllAsync(cancellationToken);
        if (customers.Count == 0)
        {
            return [];
        }

        var allAppointments = await _appointmentRepository.GetAllAsync(cancellationToken);

        // Count appointments per customer from appointments repository
        var appointmentCountByCustomer = allAppointments
            .GroupBy(a => a.CustomerId)
            .ToDictionary(g => g.Key, g => g.Count());

        var result = new List<AtRiskCustomerDto>();

        foreach (var customer in customers)
        {
            DateOnly? lastDate = customer.LastAppointment.HasValue
                ? DateOnly.FromDateTime(customer.LastAppointment.Value)
                : null;

            int daysSince = lastDate.HasValue
                ? today.DayNumber - lastDate.Value.DayNumber
                : 999;

            bool isInactive = !customer.IsActive;

            string? riskLevel;
            if (isInactive || daysSince >= 25)
            {
                riskLevel = "high";
            }
            else if (daysSince >= 14)
            {
                riskLevel = "medium";
            }
            else if (daysSince >= 7)
            {
                riskLevel = "low";
            }
            else
            {
                continue; // Not at risk
            }

            int appointmentsCount = appointmentCountByCustomer.TryGetValue(customer.Id, out var cnt) ? cnt : 0;

            string suggestedAction = riskLevel switch
            {
                "high" when appointmentsCount > 0 => "ارسال پیامک تخفیف بازگشت — بیش از ۳ هفته غایب",
                "high" => "تماس خوشآمدگویی — مشتری جدید بدون مراجعه",
                "medium" => "یادآوری نوبت + پیشنهاد سرویس مکمل",
                _ => $"پیگیری ملایم — {daysSince} روز از آخرین مراجعه"
            };

            result.Add(new AtRiskCustomerDto(
                CustomerId: customer.Id,
                CustomerName: customer.FullName,
                Phone: customer.PhoneNumber,
                LastAppointmentDate: lastDate?.ToString("yyyy-MM-dd"),
                DaysSinceVisit: daysSince,
                AppointmentsCount: appointmentsCount,
                RiskLevel: riskLevel,
                SuggestedAction: suggestedAction));
        }

        // Sort: high first, medium second, low third; within same level by daysSince descending
        var riskOrder = new Dictionary<string, int> { ["high"] = 0, ["medium"] = 1, ["low"] = 2 };
        result.Sort((a, b) =>
        {
            var levelCmp = riskOrder[a.RiskLevel].CompareTo(riskOrder[b.RiskLevel]);
            return levelCmp != 0 ? levelCmp : b.DaysSinceVisit.CompareTo(a.DaysSinceVisit);
        });

        return result;
    }

    /// <summary>
    /// Returns unbooked 30-minute time gaps across all active staff in the rolling 7-day window.
    /// </summary>
    public async Task<List<TimeGapDto>> GetGapAnalysisAsync(int maxGaps = 12, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_staffRepository is null)
        {
            return [];
        }

        var activeStaff = await _staffRepository.GetAllActiveAsync(cancellationToken);
        if (activeStaff.Count == 0)
        {
            return [];
        }

        var today = DateOnly.FromDateTime(_clock.UtcNow);
        var windowStart = today.AddDays(-(WeeklyRollingDays - 1));

        var allAppointments = await _appointmentRepository.GetAllAsync(cancellationToken);

        // All 17 slots: 09:00 to 16:30 in 30-min increments
        var slots = new List<TimeOnly>();
        for (var hour = 9; hour <= 16; hour++)
        {
            slots.Add(new TimeOnly(hour, 0));
            if (hour < 16 || true) // include 16:30 too
            {
                slots.Add(new TimeOnly(hour, 30));
            }
        }
        // Remove 17:00 — we only want up to 16:30 (17 slots: 9:00..16:30)
        slots = slots.Where(s => s <= new TimeOnly(16, 30)).ToList();

        var gaps = new List<TimeGapDto>();

        for (var i = 0; i < WeeklyRollingDays; i++)
        {
            var date = windowStart.AddDays(i);

            // Non-cancelled appointments on this date, indexed by StaffId
            var bookedByStaff = allAppointments
                .Where(a => a.TimeSlot.Date == date &&
                            a.Status != AppointmentStatus.Cancelled &&
                            a.Status != AppointmentStatus.NoShow)
                .GroupBy(a => a.StaffId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var member in activeStaff)
            {
                var booked = bookedByStaff.TryGetValue(member.Id, out var appts) ? appts : [];

                foreach (var slot in slots)
                {
                    // A slot is booked if any appointment's TimeSlot.Contains(slot)
                    var isBooked = booked.Any(a => a.TimeSlot.Contains(slot));
                    if (!isBooked)
                    {
                        gaps.Add(new TimeGapDto(
                            Id: $"gap_{date:yyyy-MM-dd}_{member.Id}_{slot:HH\\:mm}",
                            Date: date.ToString("yyyy-MM-dd"),
                            DayLabel: date.DayOfWeek.ToString(),
                            Time: slot.ToString("HH:mm"),
                            StaffId: member.Id,
                            StaffName: member.FullName,
                            DurationMinutes: 30));
                    }
                }
            }
        }

        // Sort by Date ASC then Time ASC, take first maxGaps
        gaps.Sort((a, b) =>
        {
            var dateCmp = string.Compare(a.Date, b.Date, StringComparison.Ordinal);
            return dateCmp != 0 ? dateCmp : string.Compare(a.Time, b.Time, StringComparison.Ordinal);
        });

        return gaps.Count <= maxGaps ? gaps : gaps.GetRange(0, maxGaps);
    }
}
