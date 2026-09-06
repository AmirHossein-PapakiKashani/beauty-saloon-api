using BarberSalon.Domain.Booking.Entities;
using BarberSalon.Domain.Booking.Enums;
using BarberSalon.Domain.Booking.ValueObjects;
using BarberSalon.Domain.Common;
using FluentAssertions;
using Xunit;

namespace BarberSalon.Domain.Tests.Booking;

public class AppointmentTests
{
    private readonly DateOnly _sampleDate = new(2026, 9, 15);
    private readonly TimeSlot _sampleTimeSlot = TimeSlot.Create(
        new DateOnly(2026, 9, 15),
        new TimeOnly(10, 0),
        new TimeOnly(10, 30));

    [Fact]
    public void TimeSlot_Create_WithValidTimes_CreatesSuccessfully()
    {
        var slot = TimeSlot.Create(_sampleDate, new TimeOnly(9, 0), new TimeOnly(10, 0));

        slot.Date.Should().Be(_sampleDate);
        slot.StartTime.Should().Be(new TimeOnly(9, 0));
        slot.EndTime.Should().Be(new TimeOnly(10, 0));
    }

    [Fact]
    public void TimeSlot_Create_WhenEndTimeEqualsStartTime_ThrowsDomainException()
    {
        var act = () => TimeSlot.Create(_sampleDate, new TimeOnly(10, 0), new TimeOnly(10, 0));

        act.Should().Throw<DomainException>()
            .WithMessage("*strictly after start time*");
    }

    [Fact]
    public void TimeSlot_Create_WhenEndTimeBeforeStartTime_ThrowsDomainException()
    {
        var act = () => TimeSlot.Create(_sampleDate, new TimeOnly(11, 0), new TimeOnly(10, 0));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void TimeSlot_OverlapsWith_WhenIntersecting_ReturnsTrue()
    {
        var slot1 = TimeSlot.Create(_sampleDate, new TimeOnly(10, 0), new TimeOnly(11, 0));
        var slot2 = TimeSlot.Create(_sampleDate, new TimeOnly(10, 30), new TimeOnly(11, 30));

        slot1.OverlapsWith(slot2).Should().BeTrue();
        slot2.OverlapsWith(slot1).Should().BeTrue();
    }

    [Fact]
    public void TimeSlot_OverlapsWith_WhenAdjacent_ReturnsFalse()
    {
        var slot1 = TimeSlot.Create(_sampleDate, new TimeOnly(10, 0), new TimeOnly(10, 30));
        var slot2 = TimeSlot.Create(_sampleDate, new TimeOnly(10, 30), new TimeOnly(11, 0));

        slot1.OverlapsWith(slot2).Should().BeFalse();
        slot2.OverlapsWith(slot1).Should().BeFalse();
    }

    [Fact]
    public void TimeSlot_OverlapsWith_WhenDifferentDates_ReturnsFalse()
    {
        var slot1 = TimeSlot.Create(new DateOnly(2026, 9, 15), new TimeOnly(10, 0), new TimeOnly(11, 0));
        var slot2 = TimeSlot.Create(new DateOnly(2026, 9, 16), new TimeOnly(10, 0), new TimeOnly(11, 0));

        slot1.OverlapsWith(slot2).Should().BeFalse();
    }

    [Fact]
    public void TimeSlot_Contains_ChecksPointInIntervalCorrectly()
    {
        var slot = TimeSlot.Create(_sampleDate, new TimeOnly(10, 0), new TimeOnly(11, 0));

        slot.Contains(new TimeOnly(10, 0)).Should().BeTrue();
        slot.Contains(new TimeOnly(10, 30)).Should().BeTrue();
        slot.Contains(new TimeOnly(11, 0)).Should().BeFalse();
        slot.Contains(new TimeOnly(9, 30)).Should().BeFalse();
    }

    [Fact]
    public void Appointment_Create_WithValidParameters_CreatesInPendingStatus()
    {
        var customerId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();

        var appointment = Appointment.Create(customerId, staffId, serviceId, _sampleTimeSlot, 150000m, "First haircut");

        appointment.Id.Should().NotBeEmpty();
        appointment.CustomerId.Should().Be(customerId);
        appointment.StaffId.Should().Be(staffId);
        appointment.SalonServiceId.Should().Be(serviceId);
        appointment.TimeSlot.Should().Be(_sampleTimeSlot);
        appointment.Status.Should().Be(AppointmentStatus.Pending);
        appointment.Price.Should().Be(150000m);
        appointment.Notes.Should().Be("First haircut");
    }

    [Fact]
    public void Appointment_Create_WithEmptyCustomerId_ThrowsDomainException()
    {
        var act = () => Appointment.Create(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), _sampleTimeSlot, 100m);

        act.Should().Throw<DomainException>()
            .WithMessage("*CustomerId cannot be empty*");
    }

    [Fact]
    public void Appointment_Create_WithEmptyStaffId_ThrowsDomainException()
    {
        var act = () => Appointment.Create(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), _sampleTimeSlot, 100m);

        act.Should().Throw<DomainException>()
            .WithMessage("*StaffId cannot be empty*");
    }

    [Fact]
    public void Appointment_Create_WithEmptySalonServiceId_ThrowsDomainException()
    {
        var act = () => Appointment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, _sampleTimeSlot, 100m);

        act.Should().Throw<DomainException>()
            .WithMessage("*SalonServiceId cannot be empty*");
    }

    [Fact]
    public void Appointment_Create_WithNegativePrice_ThrowsDomainException()
    {
        var act = () => Appointment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), _sampleTimeSlot, -50m);

        act.Should().Throw<DomainException>()
            .WithMessage("*Price cannot be negative*");
    }

    [Fact]
    public void Appointment_Confirm_WhenPending_TransitionsToConfirmed()
    {
        var appointment = Appointment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), _sampleTimeSlot, 100m);

        appointment.Confirm();

        appointment.Status.Should().Be(AppointmentStatus.Confirmed);
    }

    [Fact]
    public void Appointment_Confirm_WhenAlreadyConfirmed_ThrowsDomainException()
    {
        var appointment = Appointment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), _sampleTimeSlot, 100m);
        appointment.Confirm();

        var act = () => appointment.Confirm();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Appointment_Complete_WhenConfirmed_TransitionsToCompleted()
    {
        var appointment = Appointment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), _sampleTimeSlot, 100m);
        appointment.Confirm();

        appointment.Complete();

        appointment.Status.Should().Be(AppointmentStatus.Completed);
    }

    [Fact]
    public void Appointment_Complete_WhenPending_ThrowsDomainException()
    {
        var appointment = Appointment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), _sampleTimeSlot, 100m);

        var act = () => appointment.Complete();

        act.Should().Throw<DomainException>()
            .WithMessage("*Only confirmed appointments can be completed*");
    }

    [Fact]
    public void Appointment_Cancel_WhenPendingOrConfirmed_TransitionsToCancelled()
    {
        var appointment = Appointment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), _sampleTimeSlot, 100m);

        appointment.Cancel("Customer requested reschedule");

        appointment.Status.Should().Be(AppointmentStatus.Cancelled);
        appointment.Notes.Should().Contain("Customer requested reschedule");
    }

    [Fact]
    public void Appointment_Cancel_WhenCompleted_ThrowsDomainException()
    {
        var appointment = Appointment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), _sampleTimeSlot, 100m);
        appointment.Confirm();
        appointment.Complete();

        var act = () => appointment.Cancel();

        act.Should().Throw<DomainException>()
            .WithMessage("*A completed appointment cannot be cancelled*");
    }

    [Fact]
    public void Appointment_Cancel_WhenAlreadyCancelled_ThrowsDomainException()
    {
        var appointment = Appointment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), _sampleTimeSlot, 100m);
        appointment.Cancel();

        var act = () => appointment.Cancel();

        act.Should().Throw<DomainException>()
            .WithMessage("*Appointment is already cancelled*");
    }

    [Fact]
    public void Appointment_MarkNoShow_WhenConfirmed_TransitionsToNoShow()
    {
        var appointment = Appointment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), _sampleTimeSlot, 100m);
        appointment.Confirm();

        appointment.MarkNoShow();

        appointment.Status.Should().Be(AppointmentStatus.NoShow);
    }

    [Fact]
    public void Appointment_BlocksSlot_ReturnsTrueOnlyForBlockingStatusesAndMatchingTime()
    {
        var appointment = Appointment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), _sampleTimeSlot, 100m);

        // Pending blocks
        appointment.BlocksSlot(_sampleDate, new TimeOnly(10, 0)).Should().BeTrue();
        appointment.BlocksSlot(_sampleDate, new TimeOnly(11, 0)).Should().BeFalse();

        // Confirmed blocks
        appointment.Confirm();
        appointment.BlocksSlot(_sampleDate, new TimeOnly(10, 0)).Should().BeTrue();

        // Completed blocks
        appointment.Complete();
        appointment.BlocksSlot(_sampleDate, new TimeOnly(10, 0)).Should().BeTrue();

        // Cancelled does not block
        var cancelledApt = Appointment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), _sampleTimeSlot, 100m);
        cancelledApt.Cancel();
        cancelledApt.BlocksSlot(_sampleDate, new TimeOnly(10, 0)).Should().BeFalse();
    }
}
