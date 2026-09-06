using BarberSalon.Application.Booking.Interfaces;
using BarberSalon.Application.Booking.Services;
using BarberSalon.Application.Common.Exceptions;
using BarberSalon.Application.Common.Interfaces;
using BarberSalon.Application.Staff.Interfaces;
using BarberSalon.Domain.Booking.Entities;
using BarberSalon.Domain.Booking.ValueObjects;
using BarberSalon.Domain.Staff.Entities;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace BarberSalon.Application.Tests.Booking;

public class BookingServiceTests
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IStaffRepository _staffRepository;
    private readonly IClock _clock;
    private readonly BookingService _service;

    private readonly DateOnly _today = new(2026, 9, 1);
    private readonly DateOnly _futureDate = new(2026, 9, 15);

    public BookingServiceTests()
    {
        _appointmentRepository = Substitute.For<IAppointmentRepository>();
        _staffRepository = Substitute.For<IStaffRepository>();
        _clock = Substitute.For<IClock>();

        _clock.UtcNow.Returns(new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc));

        _service = new BookingService(_appointmentRepository, _staffRepository, _clock);
    }

    [Fact]
    public async Task GetAvailableTimeSlotsAsync_WhenNoStaffSpecifiedAndNoAppointments_Returns19AvailableSlots()
    {
        var staff1 = StaffMember.Create("Ali", "ali", "09121112233", "", "Barber", 5);
        var staff2 = StaffMember.Create("Reza", "reza", "09122223344", "", "Stylist", 3);
        _staffRepository.GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(new List<StaffMember> { staff1, staff2 });

        _appointmentRepository.GetByDateAsync(_futureDate, Arg.Any<CancellationToken>())
            .Returns(new List<Appointment>());

        var result = await _service.GetAvailableTimeSlotsAsync("2026-09-15");

        result.Should().HaveCount(19);
        result.Should().OnlyContain(s => s.Available);
        result.First().Time.Should().Be("09:00");
        result.Last().Time.Should().Be("18:00");
    }

    [Fact]
    public async Task GetAvailableTimeSlotsAsync_WhenAllActiveStaffBookedAtSlot_MarksOnlyThatSlotUnavailable()
    {
        var staff1 = StaffMember.Create("Ali", "ali", "09121112233", "", "Barber", 5);
        var staff2 = StaffMember.Create("Reza", "reza", "09122223344", "", "Stylist", 3);
        _staffRepository.GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(new List<StaffMember> { staff1, staff2 });

        var slotTime = TimeSlot.Create(_futureDate, new TimeOnly(10, 30), new TimeOnly(11, 0));
        var apt1 = Appointment.Create(Guid.NewGuid(), staff1.Id, Guid.NewGuid(), slotTime, 100m);
        var apt2 = Appointment.Create(Guid.NewGuid(), staff2.Id, Guid.NewGuid(), slotTime, 100m);

        _appointmentRepository.GetByDateAsync(_futureDate, Arg.Any<CancellationToken>())
            .Returns(new List<Appointment> { apt1, apt2 });

        var result = await _service.GetAvailableTimeSlotsAsync("2026-09-15");

        result.Should().HaveCount(19);
        var slot1030 = result.Single(s => s.Time == "10:30");
        slot1030.Available.Should().BeFalse();

        result.Where(s => s.Time != "10:30").Should().OnlyContain(s => s.Available);
    }

    [Fact]
    public async Task GetAvailableTimeSlotsAsync_WhenOnlyOneOfMultipleStaffBooked_SlotRemainsAvailable()
    {
        var staff1 = StaffMember.Create("Ali", "ali", "09121112233", "", "Barber", 5);
        var staff2 = StaffMember.Create("Reza", "reza", "09122223344", "", "Stylist", 3);
        _staffRepository.GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(new List<StaffMember> { staff1, staff2 });

        var slotTime = TimeSlot.Create(_futureDate, new TimeOnly(10, 30), new TimeOnly(11, 0));
        var apt1 = Appointment.Create(Guid.NewGuid(), staff1.Id, Guid.NewGuid(), slotTime, 100m);

        _appointmentRepository.GetByDateAsync(_futureDate, Arg.Any<CancellationToken>())
            .Returns(new List<Appointment> { apt1 });

        var result = await _service.GetAvailableTimeSlotsAsync("2026-09-15");

        var slot1030 = result.Single(s => s.Time == "10:30");
        slot1030.Available.Should().BeTrue();
    }

    [Fact]
    public async Task GetAvailableTimeSlotsAsync_WhenNoActiveStaffExistInSalon_ReturnsAllSlotsUnavailable()
    {
        _staffRepository.GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(new List<StaffMember>());

        var result = await _service.GetAvailableTimeSlotsAsync("2026-09-15");

        result.Should().HaveCount(19);
        result.Should().OnlyContain(s => !s.Available);
    }

    [Fact]
    public async Task GetAvailableTimeSlotsAsync_WithStaffId_WhenStaffHasAppointment_MarksSlotUnavailable()
    {
        var staff = StaffMember.Create("Ali", "ali", "09121112233", "", "Barber", 5);
        _staffRepository.GetByIdAsync(staff.Id, Arg.Any<CancellationToken>())
            .Returns(staff);

        var slotTime = TimeSlot.Create(_futureDate, new TimeOnly(14, 0), new TimeOnly(14, 30));
        var appointment = Appointment.Create(Guid.NewGuid(), staff.Id, Guid.NewGuid(), slotTime, 150m);
        appointment.Confirm();

        _appointmentRepository.GetByStaffAndDateAsync(staff.Id, _futureDate, Arg.Any<CancellationToken>())
            .Returns(new List<Appointment> { appointment });

        var result = await _service.GetAvailableTimeSlotsAsync("2026-09-15", staff.Id);

        result.Should().HaveCount(19);
        result.Single(s => s.Time == "14:00").Available.Should().BeFalse();
        result.Where(s => s.Time != "14:00").Should().OnlyContain(s => s.Available);
    }

    [Fact]
    public async Task GetAvailableTimeSlotsAsync_WithStaffId_WhenAppointmentIsCancelled_SlotRemainsAvailable()
    {
        var staff = StaffMember.Create("Ali", "ali", "09121112233", "", "Barber", 5);
        _staffRepository.GetByIdAsync(staff.Id, Arg.Any<CancellationToken>())
            .Returns(staff);

        var slotTime = TimeSlot.Create(_futureDate, new TimeOnly(14, 0), new TimeOnly(14, 30));
        var appointment = Appointment.Create(Guid.NewGuid(), staff.Id, Guid.NewGuid(), slotTime, 150m);
        appointment.Cancel();

        _appointmentRepository.GetByStaffAndDateAsync(staff.Id, _futureDate, Arg.Any<CancellationToken>())
            .Returns(new List<Appointment> { appointment });

        var result = await _service.GetAvailableTimeSlotsAsync("2026-09-15", staff.Id);

        result.Single(s => s.Time == "14:00").Available.Should().BeTrue();
    }

    [Fact]
    public async Task GetAvailableTimeSlotsAsync_WithNonExistentStaffId_ThrowsNotFoundException()
    {
        var missingStaffId = Guid.NewGuid();
        _staffRepository.GetByIdAsync(missingStaffId, Arg.Any<CancellationToken>())
            .Returns((StaffMember?)null);

        var act = () => _service.GetAvailableTimeSlotsAsync("2026-09-15", missingStaffId);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetAvailableTimeSlotsAsync_WithArchivedStaffId_ThrowsNotFoundException()
    {
        var staff = StaffMember.Create("Ali", "ali", "09121112233", "", "Barber", 5);
        staff.Archive();

        _staffRepository.GetByIdAsync(staff.Id, Arg.Any<CancellationToken>())
            .Returns(staff);

        var act = () => _service.GetAvailableTimeSlotsAsync("2026-09-15", staff.Id);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task GetAvailableTimeSlotsAsync_WhenDateStringIsNullOrEmpty_ThrowsValidationException(string? invalidDate)
    {
        var act = () => _service.GetAvailableTimeSlotsAsync(invalidDate!);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Date cannot be empty*");
    }

    [Theory]
    [InlineData("invalid-date")]
    [InlineData("2026/09/15")]
    [InlineData("15-09-2026")]
    public async Task GetAvailableTimeSlotsAsync_WhenDateFormatIsInvalid_ThrowsValidationException(string invalidDate)
    {
        var act = () => _service.GetAvailableTimeSlotsAsync(invalidDate);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Invalid date format*");
    }

    [Fact]
    public async Task GetAvailableTimeSlotsAsync_WhenDateIsInThePast_ThrowsValidationException()
    {
        var pastDate = "2026-08-31";

        var act = () => _service.GetAvailableTimeSlotsAsync(pastDate);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Date cannot be in the past*");
    }

    [Fact]
    public async Task GetAvailableTimeSlotsAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => _service.GetAvailableTimeSlotsAsync("2026-09-15", cancellationToken: cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
