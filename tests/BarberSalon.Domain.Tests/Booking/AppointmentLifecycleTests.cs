using BarberSalon.Domain.Booking.Entities;
using BarberSalon.Domain.Booking.Enums;
using BarberSalon.Domain.Booking.ValueObjects;
using BarberSalon.Domain.Common;
using FluentAssertions;
using Xunit;

namespace BarberSalon.Domain.Tests.Booking;

public class AppointmentLifecycleTests
{
    private readonly DateOnly _sampleDate = new(2026, 10, 20);
    private readonly TimeSlot _sampleTimeSlot = TimeSlot.Create(
        new DateOnly(2026, 10, 20),
        new TimeOnly(10, 0),
        new TimeOnly(11, 0));
    private readonly decimal _samplePrice = 1000000m;

    private Appointment CreateSampleAppointment(AppointmentStatus status = AppointmentStatus.Confirmed)
    {
        var appt = Appointment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            _sampleTimeSlot,
            _samplePrice,
            "Special requests");

        if (status == AppointmentStatus.Confirmed)
        {
            appt.Confirm();
        }
        return appt;
    }

    [Fact]
    public void StartService_FromConfirmed_SetsInProgress()
    {
        // Arrange
        var appt = CreateSampleAppointment(AppointmentStatus.Confirmed);

        // Act
        appt.StartService();

        // Assert
        appt.Status.Should().Be(AppointmentStatus.InProgress);
    }

    [Fact]
    public void Complete_CalculatesCommissionCorrectly()
    {
        // Arrange
        var appt = CreateSampleAppointment(AppointmentStatus.Confirmed);
        appt.StartService();

        // Act (40% commission on 1,000,000 IRR)
        appt.Complete(40m);

        // Assert
        appt.Status.Should().Be(AppointmentStatus.Completed);
        appt.CommissionRate.Should().Be(40m);
        appt.CommissionEarned.Should().Be(400000m);
    }

    [Fact]
    public void RecordDelay_IncrementsDelayMinutes()
    {
        // Arrange
        var appt = CreateSampleAppointment(AppointmentStatus.Confirmed);

        // Act
        appt.RecordDelay(15);
        appt.RecordDelay(10);

        // Assert
        appt.DelayMinutes.Should().Be(25);
    }

    [Fact]
    public void StartService_FromPending_ThrowsDomainException()
    {
        // Arrange
        var appt = CreateSampleAppointment(AppointmentStatus.Pending);

        // Act
        var act = () => appt.StartService();

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*confirmed*");
    }

    [Fact]
    public void RecordDelay_WithZeroOrNegativeMinutes_ThrowsDomainException()
    {
        // Arrange
        var appt = CreateSampleAppointment(AppointmentStatus.Confirmed);

        // Act
        var act = () => appt.RecordDelay(0);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*greater than zero*");
    }

    [Fact]
    public void BlocksSlot_WhenInProgress_ReturnsTrue()
    {
        // Arrange
        var appt = CreateSampleAppointment(AppointmentStatus.Confirmed);
        appt.StartService();

        // Act & Assert
        appt.BlocksSlot(_sampleDate, new TimeOnly(10, 30)).Should().BeTrue();
    }
}
