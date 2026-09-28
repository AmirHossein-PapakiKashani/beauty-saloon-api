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
}
