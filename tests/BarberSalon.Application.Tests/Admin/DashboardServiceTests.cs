using BarberSalon.Application.Admin.Services;
using BarberSalon.Application.Booking.Interfaces;
using BarberSalon.Application.Common.Interfaces;
using BarberSalon.Domain.Booking.Entities;
using BarberSalon.Domain.Booking.Enums;
using BarberSalon.Domain.Booking.ValueObjects;
using NSubstitute;
using Xunit;

namespace BarberSalon.Application.Tests.Admin;

public sealed class DashboardServiceTests
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IClock _clock;
    private readonly DashboardService _sut;
    private readonly DateTime _fixedNow = new(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);
    private readonly DateOnly _today = new(2026, 9, 1);

    public DashboardServiceTests()
    {
        _appointmentRepository = Substitute.For<IAppointmentRepository>();
        _clock = Substitute.For<IClock>();
        _clock.UtcNow.Returns(_fixedNow);

        _sut = new DashboardService(_appointmentRepository, _clock);
    }

    [Fact]
    public async Task GetDashboardStatsAsync_WhenNoAppointments_ReturnsAllZeros()
    {
        // Arrange
        _appointmentRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Appointment>());

        // Act
        var result = await _sut.GetDashboardStatsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TodayAppointments);
        Assert.Equal(0, result.PendingAppointments);
        Assert.Equal(0, result.WeeklyCustomers);
        Assert.Equal(0m, result.TodayRevenue);
    }

    [Fact]
    public async Task GetDashboardStatsAsync_WithTodayAppointments_CalculatesActiveStatsAndRevenueCorrectly()
    {
        // Arrange
        var customer1 = Guid.NewGuid();
        var customer2 = Guid.NewGuid();
        var customer3 = Guid.NewGuid();
        var customer4 = Guid.NewGuid();
        var customer5 = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();

        var aptPending = Appointment.Create(customer1, staffId, serviceId, TimeSlot.Create(_today, new TimeOnly(10, 0), new TimeOnly(10, 30)), 150000m);
        
        var aptConfirmed = Appointment.Create(customer2, staffId, serviceId, TimeSlot.Create(_today, new TimeOnly(11, 0), new TimeOnly(11, 30)), 250000m);
        aptConfirmed.Confirm();

        var aptCompleted = Appointment.Create(customer3, staffId, serviceId, TimeSlot.Create(_today, new TimeOnly(12, 0), new TimeOnly(12, 30)), 350000m);
        aptCompleted.Confirm();
        aptCompleted.Complete();

        var aptCancelled = Appointment.Create(customer4, staffId, serviceId, TimeSlot.Create(_today, new TimeOnly(14, 0), new TimeOnly(14, 30)), 200000m);
        aptCancelled.Cancel("Customer requested");

        var aptNoShow = Appointment.Create(customer5, staffId, serviceId, TimeSlot.Create(_today, new TimeOnly(15, 0), new TimeOnly(15, 30)), 100000m);
        aptNoShow.Confirm();
        aptNoShow.MarkNoShow();

        _appointmentRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Appointment> { aptPending, aptConfirmed, aptCompleted, aptCancelled, aptNoShow });

        // Act
        var result = await _sut.GetDashboardStatsAsync();

        // Assert
        // Today active appointments: Pending, Confirmed, Completed = 3
        Assert.Equal(3, result.TodayAppointments);
        // Pending appointments across the salon = 1
        Assert.Equal(1, result.PendingAppointments);
        // Weekly unique active customers = 3 (customer1, customer2, customer3)
        Assert.Equal(3, result.WeeklyCustomers);
        // Today revenue from Confirmed & Completed = 250000 + 350000 = 600000
        Assert.Equal(600000m, result.TodayRevenue);
    }

    [Fact]
    public async Task GetDashboardStatsAsync_WithPastAndFutureAppointments_CalculatesWeeklyCustomersAndPendingCorrectly()
    {
        // Arrange
        var customer1 = Guid.NewGuid();
        var customer2 = Guid.NewGuid();
        var customer3 = Guid.NewGuid();
        var customer4 = Guid.NewGuid();
        var customer5 = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();

        // 3 days ago (within 7-day rolling window)
        var aptPast3Days = Appointment.Create(customer1, staffId, serviceId, TimeSlot.Create(_today.AddDays(-3), new TimeOnly(10, 0), new TimeOnly(10, 30)), 100000m);
        aptPast3Days.Confirm();

        // 6 days ago (at boundary of 7-day rolling window)
        var aptPast6Days = Appointment.Create(customer2, staffId, serviceId, TimeSlot.Create(_today.AddDays(-6), new TimeOnly(11, 0), new TimeOnly(11, 30)), 200000m);
        aptPast6Days.Confirm();
        aptPast6Days.Complete();

        // 8 days ago (outside 7-day window)
        var aptPast8Days = Appointment.Create(customer3, staffId, serviceId, TimeSlot.Create(_today.AddDays(-8), new TimeOnly(12, 0), new TimeOnly(12, 30)), 300000m);
        aptPast8Days.Confirm();

        // Tomorrow (future Pending)
        var aptFuturePending = Appointment.Create(customer4, staffId, serviceId, TimeSlot.Create(_today.AddDays(1), new TimeOnly(14, 0), new TimeOnly(14, 30)), 400000m);

        // Next week (future Confirmed)
        var aptFutureConfirmed = Appointment.Create(customer5, staffId, serviceId, TimeSlot.Create(_today.AddDays(7), new TimeOnly(15, 0), new TimeOnly(15, 30)), 500000m);
        aptFutureConfirmed.Confirm();

        _appointmentRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Appointment> { aptPast3Days, aptPast6Days, aptPast8Days, aptFuturePending, aptFutureConfirmed });

        // Act
        var result = await _sut.GetDashboardStatsAsync();

        // Assert
        Assert.Equal(0, result.TodayAppointments);
        Assert.Equal(1, result.PendingAppointments); // Only aptFuturePending is Pending
        Assert.Equal(2, result.WeeklyCustomers); // customer1 (past 3 days) + customer2 (past 6 days)
        Assert.Equal(0m, result.TodayRevenue);
    }

    [Fact]
    public async Task GetDashboardStatsAsync_WithDuplicateCustomersInWeeklyWindow_CountsDistinctCustomers()
    {
        // Arrange
        var customer1 = Guid.NewGuid();
        var customer2 = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();

        var apt1 = Appointment.Create(customer1, staffId, serviceId, TimeSlot.Create(_today, new TimeOnly(10, 0), new TimeOnly(10, 30)), 100000m);
        apt1.Confirm();
        var apt2 = Appointment.Create(customer1, staffId, serviceId, TimeSlot.Create(_today.AddDays(-2), new TimeOnly(10, 0), new TimeOnly(10, 30)), 100000m);
        apt2.Confirm();
        var apt3 = Appointment.Create(customer1, staffId, serviceId, TimeSlot.Create(_today.AddDays(-4), new TimeOnly(10, 0), new TimeOnly(10, 30)), 100000m);
        apt3.Confirm();

        var apt4 = Appointment.Create(customer2, staffId, serviceId, TimeSlot.Create(_today.AddDays(-1), new TimeOnly(11, 0), new TimeOnly(11, 30)), 200000m);
        apt4.Confirm();

        _appointmentRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Appointment> { apt1, apt2, apt3, apt4 });

        // Act
        var result = await _sut.GetDashboardStatsAsync();

        // Assert
        Assert.Equal(1, result.TodayAppointments);
        Assert.Equal(0, result.PendingAppointments);
        Assert.Equal(2, result.WeeklyCustomers); // 2 distinct customers
        Assert.Equal(100000m, result.TodayRevenue);
    }

    [Fact]
    public async Task GetDashboardStatsAsync_WhenCancellationTokenIsCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => _sut.GetDashboardStatsAsync(cts.Token));
    }
}
