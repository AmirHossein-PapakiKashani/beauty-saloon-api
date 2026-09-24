using BarberSalon.Application.Booking.Interfaces;
using BarberSalon.Application.Reviews.Interfaces;
using BarberSalon.Application.Staff.Interfaces;
using BarberSalon.Application.StaffPerformance.DTOs;
using BarberSalon.Domain.Booking.Enums;

namespace BarberSalon.Application.StaffPerformance.Services;

public sealed class StaffPerformanceService(
    IStaffRepository staffRepository,
    IAppointmentRepository appointmentRepository,
    IReviewRepository reviewRepository)
{
    public async Task<List<StaffPerformanceDto>> GetStaffPerformanceAsync(
        string? from = null,
        string? to = null,
        CancellationToken ct = default)
    {
        DateOnly? fromDate = DateOnly.TryParse(from, out var f) ? f : null;
        DateOnly? toDate = DateOnly.TryParse(to, out var t) ? t : null;

        var allStaff = await staffRepository.GetAllAsync(ct);
        var allAppointments = await appointmentRepository.GetAllAsync(ct);
        var allReviews = await reviewRepository.GetAllAsync(cancellationToken: ct);

        if (fromDate.HasValue)
        {
            allAppointments = allAppointments.Where(a => a.TimeSlot.Date >= fromDate.Value).ToList();
            allReviews = allReviews.Where(r => DateOnly.FromDateTime(r.CreatedAt) >= fromDate.Value).ToList();
        }

        if (toDate.HasValue)
        {
            allAppointments = allAppointments.Where(a => a.TimeSlot.Date <= toDate.Value).ToList();
            allReviews = allReviews.Where(r => DateOnly.FromDateTime(r.CreatedAt) <= toDate.Value).ToList();
        }

        var metrics = new List<MutableMetric>();

        foreach (var staff in allStaff)
        {
            var staffAppts = allAppointments.Where(a => a.StaffId == staff.Id).ToList();
            var total = staffAppts.Count;
            var completed = staffAppts.Count(a => a.Status == AppointmentStatus.Completed);
            var confirmed = staffAppts.Count(a => a.Status == AppointmentStatus.Confirmed);
            var cancelled = staffAppts.Count(a => a.Status == AppointmentStatus.Cancelled);
            var noShow = staffAppts.Count(a => a.Status == AppointmentStatus.NoShow);
            var resolved = completed + cancelled + noShow;

            var totalRevenue = staffAppts
                .Where(a => a.Status == AppointmentStatus.Completed)
                .Sum(a => a.Price);

            var staffReviews = allReviews.Where(r => r.StaffId == staff.Id && r.Status == "published").ToList();
            var reviewCount = staffReviews.Count;
            var avgRating = reviewCount > 0
                ? Math.Round(staffReviews.Average(r => (double)r.Rating), 1)
                : 0.0;

            var completionRate = resolved > 0
                ? Math.Round((double)completed / resolved * 100, 1)
                : 0.0;
            var cancellationRate = total > 0
                ? Math.Round((double)cancelled / total * 100, 1)
                : 0.0;
            var noShowRate = total > 0
                ? Math.Round((double)noShow / total * 100, 1)
                : 0.0;

            metrics.Add(new MutableMetric
            {
                StaffId = staff.Id,
                StaffName = staff.FullName,
                Role = staff.Role,
                IsActive = staff.IsActive,
                TotalAppointments = total,
                CompletedAppointments = completed,
                ConfirmedAppointments = confirmed,
                CancelledAppointments = cancelled,
                NoShowAppointments = noShow,
                TotalRevenue = totalRevenue,
                AverageRating = avgRating,
                ReviewCount = reviewCount,
                CompletionRate = completionRate,
                CancellationRate = cancellationRate,
                NoShowRate = noShowRate
            });
        }

        var maxRevenue = metrics.Count > 0 ? metrics.Max(m => m.TotalRevenue) : 1m;
        if (maxRevenue <= 0) maxRevenue = 1m;

        foreach (var m in metrics)
        {
            var ratingNorm = m.AverageRating > 0 ? (m.AverageRating / 5.0) * 100.0 : 50.0;
            var revenueNorm = (double)(m.TotalRevenue / maxRevenue) * 100.0;
            var cancelPenalty = Math.Max(0.0, 100.0 - m.CancellationRate * 3.0);
            var noShowPenalty = Math.Max(0.0, 100.0 - m.NoShowRate * 4.0);

            m.PerformanceScore = (int)Math.Round(
                m.CompletionRate * 0.35 +
                ratingNorm * 0.25 +
                revenueNorm * 0.20 +
                cancelPenalty * 0.12 +
                noShowPenalty * 0.08
            );
        }

        var ranked = metrics.OrderByDescending(m => m.PerformanceScore).ToList();
        for (int i = 0; i < ranked.Count; i++)
        {
            ranked[i].Rank = i + 1;
        }

        return ranked.Select(m => new StaffPerformanceDto(
            m.StaffId,
            m.StaffName,
            m.Role,
            m.IsActive,
            m.TotalAppointments,
            m.CompletedAppointments,
            m.ConfirmedAppointments,
            m.CancelledAppointments,
            m.NoShowAppointments,
            m.TotalRevenue,
            m.AverageRating,
            m.ReviewCount,
            m.CompletionRate,
            m.CancellationRate,
            m.NoShowRate,
            m.PerformanceScore,
            m.Rank
        )).ToList();
    }

    private sealed class MutableMetric
    {
        public Guid StaffId { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int TotalAppointments { get; set; }
        public int CompletedAppointments { get; set; }
        public int ConfirmedAppointments { get; set; }
        public int CancelledAppointments { get; set; }
        public int NoShowAppointments { get; set; }
        public decimal TotalRevenue { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public double CompletionRate { get; set; }
        public double CancellationRate { get; set; }
        public double NoShowRate { get; set; }
        public int PerformanceScore { get; set; }
        public int Rank { get; set; }
    }
}
