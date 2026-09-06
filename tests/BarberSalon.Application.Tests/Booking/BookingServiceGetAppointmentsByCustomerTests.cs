using BarberSalon.Application.Auth.Interfaces;
using BarberSalon.Application.Booking.Interfaces;
using BarberSalon.Application.Booking.Services;
using BarberSalon.Application.Common.Exceptions;
using BarberSalon.Application.Common.Interfaces;
using BarberSalon.Application.SalonServices.Interfaces;
using BarberSalon.Application.Staff.Interfaces;
using BarberSalon.Domain.Auth.Entities;
using BarberSalon.Domain.Auth.Enums;
using BarberSalon.Domain.Booking.Entities;
using BarberSalon.Domain.Booking.ValueObjects;
using BarberSalon.Domain.SalonServices.Entities;
using BarberSalon.Domain.Staff.Entities;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace BarberSalon.Application.Tests.Booking;

public class BookingServiceGetAppointmentsByCustomerTests
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IStaffRepository _staffRepository;
    private readonly ISalonServiceRepository _salonServiceRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly BookingService _service;

    public BookingServiceGetAppointmentsByCustomerTests()
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
    public async Task GetAppointmentsByCustomerIdAsync_WithValidCustomerHavingAppointments_ReturnsMappedAppointmentDtosInOrder()
    {
        // Arrange
        var customer = User.Create("09121112233", DateTime.UtcNow, UserRole.Customer, "علیرضا نادری");
        _userRepository.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>())
            .Returns(customer);

        var staff = StaffMember.Create("استاد احمدی", "ahmadi", "09123334455", "", "Barber", 5);
        _staffRepository.GetByIdAsync(staff.Id, Arg.Any<CancellationToken>())
            .Returns(staff);

        var salonService = SalonService.Create("کوتاهی مو", "اصلاح سر", 30, 150000m, "Haircut");
        _salonServiceRepository.GetByIdAsync(salonService.Id, Arg.Any<CancellationToken>())
            .Returns(salonService);

        var date1 = new DateOnly(2026, 9, 20);
        var slot1 = TimeSlot.Create(date1, new TimeOnly(11, 0), new TimeOnly(11, 30));
        var apt1 = Appointment.Create(customer.Id, staff.Id, salonService.Id, slot1, 150000m, "نوبت اول");

        var date2 = new DateOnly(2026, 9, 10);
        var slot2 = TimeSlot.Create(date2, new TimeOnly(14, 0), new TimeOnly(14, 30));
        var apt2 = Appointment.Create(customer.Id, staff.Id, salonService.Id, slot2, 150000m, "نوبت دوم");

        _appointmentRepository.GetByCustomerIdAsync(customer.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Appointment> { apt1, apt2 });

        // Act
        var result = await _service.GetAppointmentsByCustomerIdAsync(customer.Id);

        // Assert
        result.Should().HaveCount(2);

        var dto1 = result[0];
        dto1.Id.Should().Be(apt1.Id);
        dto1.CustomerId.Should().Be(customer.Id);
        dto1.CustomerName.Should().Be("علیرضا نادری");
        dto1.StaffId.Should().Be(staff.Id);
        dto1.StaffName.Should().Be("استاد احمدی");
        dto1.ServiceId.Should().Be(salonService.Id);
        dto1.ServiceName.Should().Be("کوتاهی مو");
        dto1.Date.Should().Be("2026-09-20");
        dto1.Time.Should().Be("11:00");
        dto1.Status.Should().Be("pending");
        dto1.Price.Should().Be(150000m);
        dto1.Notes.Should().Be("نوبت اول");

        var dto2 = result[1];
        dto2.Id.Should().Be(apt2.Id);
        dto2.Date.Should().Be("2026-09-10");
        dto2.Time.Should().Be("14:00");
        dto2.Notes.Should().Be("نوبت دوم");
    }

    [Fact]
    public async Task GetAppointmentsByCustomerIdAsync_WithValidCustomerHavingNoAppointments_ReturnsEmptyList()
    {
        // Arrange
        var customer = User.Create("09121112233", DateTime.UtcNow, UserRole.Customer, "علیرضا نادری");
        _userRepository.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>())
            .Returns(customer);

        _appointmentRepository.GetByCustomerIdAsync(customer.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Appointment>());

        // Act
        var result = await _service.GetAppointmentsByCustomerIdAsync(customer.Id);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAppointmentsByCustomerIdAsync_WithEmptyCustomerId_ThrowsValidationException()
    {
        // Act
        var act = () => _service.GetAppointmentsByCustomerIdAsync(Guid.Empty);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*CustomerId cannot be empty*");
    }

    [Fact]
    public async Task GetAppointmentsByCustomerIdAsync_WithNonExistentCustomerId_ThrowsNotFoundException()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        _userRepository.GetByIdAsync(customerId, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        // Act
        var act = () => _service.GetAppointmentsByCustomerIdAsync(customerId);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetAppointmentsByCustomerIdAsync_WithInactiveCustomer_ThrowsNotFoundException()
    {
        // Arrange
        var customer = User.Create("09121112233", DateTime.UtcNow, UserRole.Customer, "علیرضا نادری");
        customer.Archive();

        _userRepository.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>())
            .Returns(customer);

        // Act
        var act = () => _service.GetAppointmentsByCustomerIdAsync(customer.Id);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetAppointmentsByCustomerIdAsync_WhenStaffOrServiceNotFound_UsesFallbackNames()
    {
        // Arrange
        var customer = User.Create("09121112233", DateTime.UtcNow, UserRole.Customer, "علیرضا نادری");
        _userRepository.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>())
            .Returns(customer);

        var staffId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        _staffRepository.GetByIdAsync(staffId, Arg.Any<CancellationToken>())
            .Returns((StaffMember?)null);
        _salonServiceRepository.GetByIdAsync(serviceId, Arg.Any<CancellationToken>())
            .Returns((SalonService?)null);

        var date = new DateOnly(2026, 9, 20);
        var slot = TimeSlot.Create(date, new TimeOnly(11, 0), new TimeOnly(11, 30));
        var appointment = Appointment.Create(customer.Id, staffId, serviceId, slot, 100000m);

        _appointmentRepository.GetByCustomerIdAsync(customer.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Appointment> { appointment });

        // Act
        var result = await _service.GetAppointmentsByCustomerIdAsync(customer.Id);

        // Assert
        result.Should().HaveCount(1);
        result[0].StaffName.Should().Be("پرسنل");
        result[0].ServiceName.Should().Be("سرویس");
    }

    [Fact]
    public async Task GetAppointmentsByCustomerIdAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = () => _service.GetAppointmentsByCustomerIdAsync(Guid.NewGuid(), cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
