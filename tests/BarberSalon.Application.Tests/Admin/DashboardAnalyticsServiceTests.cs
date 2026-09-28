using BarberSalon.Application.Admin.Services;
using BarberSalon.Application.Booking.Interfaces;
using BarberSalon.Application.Common.Interfaces;
using BarberSalon.Application.Customers.Interfaces;
using BarberSalon.Application.Staff.Interfaces;
using BarberSalon.Domain.Booking.Entities;
using BarberSalon.Domain.Booking.Enums;
using BarberSalon.Domain.Booking.ValueObjects;
using BarberSalon.Domain.Customers.Entities;
using BarberSalon.Domain.Staff.Entities;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace BarberSalon.Application.Tests.Admin;

/// <summary>
/// Tests for the three analytics methods added to DashboardService:
/// GetRevenueForecastAsync, GetAtRiskCustomersAsync, GetGapAnalysisAsync.
/// </summary>
public sealed class DashboardAnalyticsServiceTests
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IStaffRepository _staffRepository;
    private readonly IClock _clock;
    private readonly DashboardService _sut;

    // Fixed "today" = 2026-09-29. Rolling window = 2026-09-23 to 2026-09-29 (7 days).
    private readonly DateTime _fixedNow = new(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc);
    private readonly DateOnly _today = new(2026, 9, 29);

    public DashboardAnalyticsServiceTests()
    {
        _appointmentRepository = Substitute.For<IAppointmentRepository>();
        _customerRepository = Substitute.For<ICustomerRepository>();
        _staffRepository = Substitute.For<IStaffRepository>();
        _clock = Substitute.For<IClock>();
        _clock.UtcNow.Returns(_fixedNow);

        _sut = new DashboardService(
            _appointmentRepository,
            _clock,
            _customerRepository,
            staffRepository: _staffRepository);
    }

    // ─── GetRevenueForecastAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetRevenueForecastAsync_ConfirmedAppointmentInWindow_CountsRevenueAndExactly7DailyBreakdownEntries()
    {
        // Arrange: one Confirmed appointment on today
        var customerId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();

        var confirmedAppt = Appointment.Create(
            customerId, staffId, serviceId,
            TimeSlot.Create(_today, new TimeOnly(10, 0), new TimeOnly(10, 30)),
            200_000m);
        confirmedAppt.Confirm();

        _appointmentRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Appointment> { confirmedAppt });

        // Act
        var result = await _sut.GetRevenueForecastAsync();

        // Assert
        result.Should().NotBeNull();
        result.DailyBreakdown.Should().HaveCount(7);
        result.ConfirmedRevenue.Should().Be(200_000m);
        result.PendingRevenue.Should().Be(0m);
        result.TotalExpected.Should().Be(200_000m);
        result.ConfirmedCount.Should().Be(1);
        result.PendingCount.Should().Be(0);
        result.CancelledCount.Should().Be(0);

        // The day entry for today should have Amount = 200000
        var todayEntry = result.DailyBreakdown.Single(d => d.Date == _today.ToString("yyyy-MM-dd"));
        todayEntry.Amount.Should().Be(200_000m);
        todayEntry.DayLabel.Should().Be(_today.DayOfWeek.ToString());
    }

    // ─── GetAtRiskCustomersAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetAtRiskCustomersAsync_WhenNoCustomers_ReturnsEmptyList()
    {
        // Arrange
        _customerRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Customer>());
        _appointmentRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Appointment>());

        // Act
        var result = await _sut.GetAtRiskCustomersAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAtRiskCustomersAsync_CalculatesRiskLevelsAndSortsCorrectly()
    {
        // Arrange
        // Customer 1: high risk (daysSince = 30 >= 25, 2 appointments)
        var c1 = Customer.Create("مشتری یک", "09121111111");
        c1.RecordAppointment(_fixedNow.AddDays(-30));
        c1.RecordAppointment(_fixedNow.AddDays(-30));

        // Customer 2: high risk (inactive customer profile, 0 appointments)
        var c2 = Customer.Create("مشتری غیرفعال", "09122222222");
        c2.Archive();

        // Customer 3: medium risk (daysSince = 16, 1 appointment)
        var c3 = Customer.Create("مشتری متوسط", "09123333333");
        c3.RecordAppointment(_fixedNow.AddDays(-16));

        // Customer 4: low risk (daysSince = 8, 1 appointment)
        var c4 = Customer.Create("مشتری کم‌خطر", "09124444444");
        c4.RecordAppointment(_fixedNow.AddDays(-8));

        // Customer 5: safe (daysSince = 2 < 7) -> should be excluded
        var c5 = Customer.Create("مشتری امن", "09125555555");
        c5.RecordAppointment(_fixedNow.AddDays(-2));

        _customerRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Customer> { c1, c2, c3, c4, c5 });

        // Dummy appointment records to simulate appointment repo counts for c1, c3, c4
        var staffId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var apt1a = Appointment.Create(c1.Id, staffId, serviceId, TimeSlot.Create(_today.AddDays(-30), new TimeOnly(10, 0), new TimeOnly(10, 30)), 100_000m);
        var apt1b = Appointment.Create(c1.Id, staffId, serviceId, TimeSlot.Create(_today.AddDays(-30), new TimeOnly(11, 0), new TimeOnly(11, 30)), 100_000m);
        var apt3 = Appointment.Create(c3.Id, staffId, serviceId, TimeSlot.Create(_today.AddDays(-16), new TimeOnly(10, 0), new TimeOnly(10, 30)), 100_000m);
        var apt4 = Appointment.Create(c4.Id, staffId, serviceId, TimeSlot.Create(_today.AddDays(-8), new TimeOnly(10, 0), new TimeOnly(10, 30)), 100_000m);
        var apt5 = Appointment.Create(c5.Id, staffId, serviceId, TimeSlot.Create(_today.AddDays(-2), new TimeOnly(10, 0), new TimeOnly(10, 30)), 100_000m);

        _appointmentRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Appointment> { apt1a, apt1b, apt3, apt4, apt5 });

        // Act
        var result = await _sut.GetAtRiskCustomersAsync();

        // Assert
        result.Should().HaveCount(4); // c5 excluded

        // Check order: high risk first, then medium, then low
        result[0].RiskLevel.Should().Be("high");
        result[1].RiskLevel.Should().Be("high");
        result[2].RiskLevel.Should().Be("medium");
        result[3].RiskLevel.Should().Be("low");

        // High risk customer with visits
        var r1 = result.Single(r => r.CustomerId == c1.Id);
        r1.RiskLevel.Should().Be("high");
        r1.DaysSinceVisit.Should().Be(30);
        r1.AppointmentsCount.Should().Be(2);
        r1.SuggestedAction.Should().Be("ارسال پیامک تخفیف بازگشت — بیش از ۳ هفته غایب");

        // High risk inactive customer without visits
        var r2 = result.Single(r => r.CustomerId == c2.Id);
        r2.RiskLevel.Should().Be("high");
        r2.AppointmentsCount.Should().Be(0);
        r2.SuggestedAction.Should().Be("تماس خوش‌آمدگویی — مشتری جدید بدون مراجعه");

        // Medium risk customer
        var r3 = result.Single(r => r.CustomerId == c3.Id);
        r3.RiskLevel.Should().Be("medium");
        r3.DaysSinceVisit.Should().Be(16);
        r3.SuggestedAction.Should().Be("یادآوری نوبت + پیشنهاد سرویس مکمل");

        // Low risk customer
        var r4 = result.Single(r => r.CustomerId == c4.Id);
        r4.RiskLevel.Should().Be("low");
        r4.DaysSinceVisit.Should().Be(8);
        r4.SuggestedAction.Should().Contain("8");
    }

    // ─── GetGapAnalysisAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetGapAnalysisAsync_WhenNoActiveStaff_ReturnsEmptyList()
    {
        // Arrange
        _staffRepository.GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(new List<StaffMember>());
        _appointmentRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Appointment>());

        // Act
        var result = await _sut.GetGapAnalysisAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetGapAnalysisAsync_WithBookedAppointments_ExcludesBookedSlotsAndHonorsMaxGaps()
    {
        // Arrange
        var staff = StaffMember.Create("آرایشگر یک", "stylist-1", "09121111111", "بیو", "آرایشگر", 5);
        _staffRepository.GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(new List<StaffMember> { staff });

        // Book slot 10:00 on today for this staff
        var customerId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var bookedApt = Appointment.Create(
            customerId, staff.Id, serviceId,
            TimeSlot.Create(_today, new TimeOnly(10, 0), new TimeOnly(10, 30)),
            200_000m);
        bookedApt.Confirm();

        _appointmentRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Appointment> { bookedApt });

        // Act
        var result = await _sut.GetGapAnalysisAsync(maxGaps: 5);

        // Assert
        result.Should().HaveCount(5);
        result.All(g => g.StaffId == staff.Id).Should().BeTrue();
        result.All(g => g.DurationMinutes == 30).Should().BeTrue();

        // Check that booked 10:00 on _today is NOT in the gap list for that day
        var todayGaps = result.Where(g => g.Date == _today.ToString("yyyy-MM-dd")).ToList();
        todayGaps.Should().NotContain(g => g.Time == "10:00");
    }
}
