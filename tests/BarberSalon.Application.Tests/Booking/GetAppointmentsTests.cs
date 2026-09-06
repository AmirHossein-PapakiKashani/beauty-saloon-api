using BarberSalon.Application.Auth.Interfaces;
using BarberSalon.Application.Booking.Interfaces;
using BarberSalon.Application.Booking.Services;
using BarberSalon.Application.Common.Interfaces;
using BarberSalon.Application.SalonServices.Interfaces;
using BarberSalon.Application.Staff.Interfaces;
using BarberSalon.Domain.Auth.Entities;
using BarberSalon.Domain.Auth.Enums;
using BarberSalon.Domain.Booking.Entities;
using BarberSalon.Domain.Booking.Enums;
using BarberSalon.Domain.Booking.ValueObjects;
using BarberSalon.Domain.SalonServices.Entities;
using BarberSalon.Domain.Staff.Entities;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace BarberSalon.Application.Tests.Booking;

public class GetAppointmentsTests
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IStaffRepository _staffRepository;
    private readonly ISalonServiceRepository _salonServiceRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly BookingService _service;

    public GetAppointmentsTests()
    {
        _appointmentRepository = Substitute.For<IAppointmentRepository>();
        _staffRepository = Substitute.For<IStaffRepository>();
        _salonServiceRepository = Substitute.For<ISalonServiceRepository>();
        _userRepository = Substitute.For<IUserRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _clock = Substitute.For<IClock>();

        _clock.UtcNow.Returns(new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc));

        _service = new BookingService(
            _appointmentRepository,
            _staffRepository,
            _salonServiceRepository,
            _userRepository,
            _unitOfWork,
            _clock);
    }

    [Fact]
    public async Task GetAllAppointmentsAsync_WhenAppointmentsExist_ReturnsMappedAppointmentDtos()
    {
        // Arrange
        var customer1 = User.Create("09121111111", DateTime.UtcNow, UserRole.Customer, "رضا احمدی");
        var customer2 = User.Create("09122222222", DateTime.UtcNow, UserRole.Customer, "مهدی حسینی");

        var staff1 = StaffMember.Create("استاد برتر", "ostad-1", "09123333333", "توضیح", "آرایشگر", 5);
        var staff2 = StaffMember.Create("استاد دوم", "ostad-2", "09124444444", "توضیح", "استایلیست", 3);

        var service1 = SalonService.Create("کوتاهی مو", "کوتاهی حرفه‌ای", 30, 200000m, "haircut");
        var service2 = SalonService.Create("اصلاح ریش", "اصلاح با تیغ", 20, 100000m, "beard");

        var slot1 = TimeSlot.Create(new DateOnly(2026, 9, 10), new TimeOnly(10, 0), new TimeOnly(10, 30));
        var apt1 = Appointment.Create(customer1.Id, staff1.Id, service1.Id, slot1, 200000m, "یادداشت ۱");

        var slot2 = TimeSlot.Create(new DateOnly(2026, 9, 9), new TimeOnly(14, 0), new TimeOnly(14, 20));
        var apt2 = Appointment.Create(customer2.Id, staff2.Id, service2.Id, slot2, 100000m, "یادداشت ۲");

        _appointmentRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Appointment> { apt1, apt2 });

        _userRepository.GetByIdAsync(customer1.Id, Arg.Any<CancellationToken>()).Returns(customer1);
        _userRepository.GetByIdAsync(customer2.Id, Arg.Any<CancellationToken>()).Returns(customer2);

        _staffRepository.GetByIdAsync(staff1.Id, Arg.Any<CancellationToken>()).Returns(staff1);
        _staffRepository.GetByIdAsync(staff2.Id, Arg.Any<CancellationToken>()).Returns(staff2);

        _salonServiceRepository.GetByIdAsync(service1.Id, Arg.Any<CancellationToken>()).Returns(service1);
        _salonServiceRepository.GetByIdAsync(service2.Id, Arg.Any<CancellationToken>()).Returns(service2);

        // Act
        var result = await _service.GetAllAppointmentsAsync();

        // Assert
        result.Should().HaveCount(2);

        result[0].Id.Should().Be(apt1.Id);
        result[0].CustomerId.Should().Be(customer1.Id);
        result[0].CustomerName.Should().Be("رضا احمدی");
        result[0].StaffId.Should().Be(staff1.Id);
        result[0].StaffName.Should().Be("استاد برتر");
        result[0].ServiceId.Should().Be(service1.Id);
        result[0].ServiceName.Should().Be("کوتاهی مو");
        result[0].Date.Should().Be("2026-09-10");
        result[0].Time.Should().Be("10:00");
        result[0].Status.Should().Be("pending");
        result[0].Price.Should().Be(200000m);
        result[0].Notes.Should().Be("یادداشت ۱");

        result[1].Id.Should().Be(apt2.Id);
        result[1].CustomerName.Should().Be("مهدی حسینی");
        result[1].StaffName.Should().Be("استاد دوم");
        result[1].ServiceName.Should().Be("اصلاح ریش");
        result[1].Date.Should().Be("2026-09-09");
        result[1].Time.Should().Be("14:00");
        result[1].Price.Should().Be(100000m);
    }

    [Fact]
    public async Task GetAllAppointmentsAsync_WhenNoAppointmentsExist_ReturnsEmptyList()
    {
        // Arrange
        _appointmentRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Appointment>());

        // Act
        var result = await _service.GetAllAppointmentsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();

        await _userRepository.DidNotReceiveWithAnyArgs().GetByIdAsync(Arg.Any<Guid>());
        await _staffRepository.DidNotReceiveWithAnyArgs().GetByIdAsync(Arg.Any<Guid>());
        await _salonServiceRepository.DidNotReceiveWithAnyArgs().GetByIdAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task GetAllAppointmentsAsync_WhenRelatedEntitiesMissing_FallsBackToDefaultNames()
    {
        // Arrange
        var missingCustomerId = Guid.NewGuid();
        var missingStaffId = Guid.NewGuid();
        var missingServiceId = Guid.NewGuid();

        var slot = TimeSlot.Create(new DateOnly(2026, 9, 12), new TimeOnly(11, 0), new TimeOnly(11, 30));
        var apt = Appointment.Create(missingCustomerId, missingStaffId, missingServiceId, slot, 50000m);

        _appointmentRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Appointment> { apt });

        _userRepository.GetByIdAsync(missingCustomerId, Arg.Any<CancellationToken>()).Returns((User?)null);
        _staffRepository.GetByIdAsync(missingStaffId, Arg.Any<CancellationToken>()).Returns((StaffMember?)null);
        _salonServiceRepository.GetByIdAsync(missingServiceId, Arg.Any<CancellationToken>()).Returns((SalonService?)null);

        // Act
        var result = await _service.GetAllAppointmentsAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].CustomerName.Should().Be("مشتری");
        result[0].StaffName.Should().Be("پرسنل");
        result[0].ServiceName.Should().Be("سرویس");
    }

    [Fact]
    public async Task GetAllAppointmentsAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = () => _service.GetAllAppointmentsAsync(cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetAllAppointmentsAsync_PreservesOrderFromRepository()
    {
        // Arrange
        var customer = User.Create("09121112233", DateTime.UtcNow, UserRole.Customer, "مشتری تست");
        var staff = StaffMember.Create("استاد تست", "ostad-test", "09123334455", "", "آرایشگر", 2);
        var service = SalonService.Create("خدمت تست", "توضیح", 30, 80000m, "hair");

        var slot1 = TimeSlot.Create(new DateOnly(2026, 9, 20), new TimeOnly(16, 0), new TimeOnly(16, 30));
        var apt1 = Appointment.Create(customer.Id, staff.Id, service.Id, slot1, 80000m);

        var slot2 = TimeSlot.Create(new DateOnly(2026, 9, 10), new TimeOnly(12, 0), new TimeOnly(12, 30));
        var apt2 = Appointment.Create(customer.Id, staff.Id, service.Id, slot2, 80000m);

        _appointmentRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Appointment> { apt1, apt2 });

        _userRepository.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);
        _staffRepository.GetByIdAsync(staff.Id, Arg.Any<CancellationToken>()).Returns(staff);
        _salonServiceRepository.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);

        // Act
        var result = await _service.GetAllAppointmentsAsync();

        // Assert
        result.Should().HaveCount(2);
        result[0].Id.Should().Be(apt1.Id);
        result[1].Id.Should().Be(apt2.Id);
    }
}
