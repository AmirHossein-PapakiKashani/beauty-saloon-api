using BarberSalon.Application.Auth.Interfaces;
using BarberSalon.Application.Booking.DTOs;
using BarberSalon.Application.Booking.Interfaces;
using BarberSalon.Application.Booking.Services;
using BarberSalon.Application.Common.Exceptions;
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

public class BookingServiceCreateAppointmentTests
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IStaffRepository _staffRepository;
    private readonly ISalonServiceRepository _salonServiceRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly BookingService _service;

    private readonly DateTime _currentUtc = new(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);
    private readonly string _validDateStr = "2026-09-10";
    private readonly string _validTimeStr = "10:00";

    private readonly User _customer;
    private readonly StaffMember _staff;
    private readonly SalonService _salonService;

    public BookingServiceCreateAppointmentTests()
    {
        _appointmentRepository = Substitute.For<IAppointmentRepository>();
        _staffRepository = Substitute.For<IStaffRepository>();
        _salonServiceRepository = Substitute.For<ISalonServiceRepository>();
        _userRepository = Substitute.For<IUserRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _clock = Substitute.For<IClock>();

        _clock.UtcNow.Returns(_currentUtc);

        _customer = User.Create("09121112233", _currentUtc, UserRole.Customer, "علی احمدی");
        _salonService = SalonService.Create("کوتاهی مو", "کوتاهی و استایل", 30, 150000m, "Haircut");
        _staff = StaffMember.Create(
            "آقای کریمی",
            "karimi",
            "09123334455",
            "آرایشگر ماهر",
            "Barber",
            5,
            serviceIds: new List<Guid> { _salonService.Id });

        _userRepository.GetByIdAsync(_customer.Id, Arg.Any<CancellationToken>()).Returns(_customer);
        _staffRepository.GetByIdAsync(_staff.Id, Arg.Any<CancellationToken>()).Returns(_staff);
        _salonServiceRepository.GetByIdAsync(_salonService.Id, Arg.Any<CancellationToken>()).Returns(_salonService);
        _appointmentRepository.GetByStaffAndDateAsync(_staff.Id, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new List<Appointment>());

        _service = new BookingService(
            _appointmentRepository,
            _staffRepository,
            _salonServiceRepository,
            _userRepository,
            _unitOfWork,
            _clock);
    }

    [Fact]
    public async Task CreateAppointmentAsync_WithValidRequest_ReturnsCreatedAppointmentAndBookingCode()
    {
        var request = new CreateAppointmentRequest(
            _customer.Id,
            _staff.Id,
            _salonService.Id,
            _validDateStr,
            _validTimeStr,
            "لطفاً با قیچی کوتاه شود");

        var response = await _service.CreateAppointmentAsync(request);

        response.Should().NotBeNull();
        response.BookingCode.Should().StartWith("#");
        response.BookingCode.Length.Should().Be(7);

        var apt = response.Appointment;
        apt.Id.Should().NotBeEmpty();
        apt.CustomerId.Should().Be(_customer.Id);
        apt.CustomerName.Should().Be(_customer.FullName);
        apt.StaffId.Should().Be(_staff.Id);
        apt.StaffName.Should().Be(_staff.FullName);
        apt.ServiceId.Should().Be(_salonService.Id);
        apt.ServiceName.Should().Be(_salonService.Name);
        apt.Date.Should().Be(_validDateStr);
        apt.Time.Should().Be(_validTimeStr);
        apt.Status.Should().Be("pending");
        apt.Price.Should().Be(150000m);
        apt.Notes.Should().Be("لطفاً با قیچی کوتاه شود");

        await _appointmentRepository.Received(1).AddAsync(Arg.Is<Appointment>(a =>
            a.CustomerId == _customer.Id &&
            a.StaffId == _staff.Id &&
            a.SalonServiceId == _salonService.Id &&
            a.Price == 150000m &&
            a.Status == AppointmentStatus.Pending), Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAppointmentAsync_WhenRequestIsNull_ThrowsValidationException()
    {
        var act = () => _service.CreateAppointmentAsync(null!);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Request body cannot be null*");
    }

    [Fact]
    public async Task CreateAppointmentAsync_WhenCustomerIdIsEmpty_ThrowsValidationException()
    {
        var request = new CreateAppointmentRequest(
            Guid.Empty,
            _staff.Id,
            _salonService.Id,
            _validDateStr,
            _validTimeStr);

        var act = () => _service.CreateAppointmentAsync(request);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*CustomerId is required*");
    }

    [Fact]
    public async Task CreateAppointmentAsync_WhenStaffIdIsEmpty_ThrowsValidationException()
    {
        var request = new CreateAppointmentRequest(
            _customer.Id,
            Guid.Empty,
            _salonService.Id,
            _validDateStr,
            _validTimeStr);

        var act = () => _service.CreateAppointmentAsync(request);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*StaffId is required*");
    }

    [Fact]
    public async Task CreateAppointmentAsync_WhenServiceIdIsEmpty_ThrowsValidationException()
    {
        var request = new CreateAppointmentRequest(
            _customer.Id,
            _staff.Id,
            Guid.Empty,
            _validDateStr,
            _validTimeStr);

        var act = () => _service.CreateAppointmentAsync(request);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*ServiceId is required*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task CreateAppointmentAsync_WhenDateIsEmpty_ThrowsValidationException(string? invalidDate)
    {
        var request = new CreateAppointmentRequest(
            _customer.Id,
            _staff.Id,
            _salonService.Id,
            invalidDate!,
            _validTimeStr);

        var act = () => _service.CreateAppointmentAsync(request);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Date cannot be empty*");
    }

    [Theory]
    [InlineData("2026/09/10")]
    [InlineData("10-09-2026")]
    [InlineData("invalid-date")]
    public async Task CreateAppointmentAsync_WhenDateFormatIsInvalid_ThrowsValidationException(string invalidDate)
    {
        var request = new CreateAppointmentRequest(
            _customer.Id,
            _staff.Id,
            _salonService.Id,
            invalidDate,
            _validTimeStr);

        var act = () => _service.CreateAppointmentAsync(request);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Invalid date format*");
    }

    [Fact]
    public async Task CreateAppointmentAsync_WhenDateIsInThePast_ThrowsValidationException()
    {
        var pastDate = "2026-08-30";
        var request = new CreateAppointmentRequest(
            _customer.Id,
            _staff.Id,
            _salonService.Id,
            pastDate,
            _validTimeStr);

        var act = () => _service.CreateAppointmentAsync(request);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Appointment date cannot be in the past*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task CreateAppointmentAsync_WhenTimeIsEmpty_ThrowsValidationException(string? invalidTime)
    {
        var request = new CreateAppointmentRequest(
            _customer.Id,
            _staff.Id,
            _salonService.Id,
            _validDateStr,
            invalidTime!);

        var act = () => _service.CreateAppointmentAsync(request);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Time cannot be empty*");
    }

    [Theory]
    [InlineData("25:00")]
    [InlineData("10:65")]
    [InlineData("morning")]
    public async Task CreateAppointmentAsync_WhenTimeFormatIsInvalid_ThrowsValidationException(string invalidTime)
    {
        var request = new CreateAppointmentRequest(
            _customer.Id,
            _staff.Id,
            _salonService.Id,
            _validDateStr,
            invalidTime);

        var act = () => _service.CreateAppointmentAsync(request);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Invalid time format*");
    }

    [Theory]
    [InlineData("08:30")]
    [InlineData("18:30")]
    [InlineData("20:00")]
    public async Task CreateAppointmentAsync_WhenTimeIsOutsideOperatingHours_ThrowsValidationException(string outsideTime)
    {
        var request = new CreateAppointmentRequest(
            _customer.Id,
            _staff.Id,
            _salonService.Id,
            _validDateStr,
            outsideTime);

        var act = () => _service.CreateAppointmentAsync(request);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Appointment time must be between*");
    }

    [Fact]
    public async Task CreateAppointmentAsync_WhenCustomerDoesNotExist_ThrowsNotFoundException()
    {
        var nonExistentCustomer = Guid.NewGuid();
        _userRepository.GetByIdAsync(nonExistentCustomer, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var request = new CreateAppointmentRequest(
            nonExistentCustomer,
            _staff.Id,
            _salonService.Id,
            _validDateStr,
            _validTimeStr);

        var act = () => _service.CreateAppointmentAsync(request);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*{nameof(User)}*");
    }

    [Fact]
    public async Task CreateAppointmentAsync_WhenCustomerIsArchived_ThrowsNotFoundException()
    {
        var archivedCustomer = User.Create("09129998877", _currentUtc, UserRole.Customer, "کاربر غیرفعال");
        archivedCustomer.Archive();

        _userRepository.GetByIdAsync(archivedCustomer.Id, Arg.Any<CancellationToken>())
            .Returns(archivedCustomer);

        var request = new CreateAppointmentRequest(
            archivedCustomer.Id,
            _staff.Id,
            _salonService.Id,
            _validDateStr,
            _validTimeStr);

        var act = () => _service.CreateAppointmentAsync(request);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*{nameof(User)}*");
    }

    [Fact]
    public async Task CreateAppointmentAsync_WhenStaffDoesNotExist_ThrowsNotFoundException()
    {
        var missingStaffId = Guid.NewGuid();
        _staffRepository.GetByIdAsync(missingStaffId, Arg.Any<CancellationToken>())
            .Returns((StaffMember?)null);

        var request = new CreateAppointmentRequest(
            _customer.Id,
            missingStaffId,
            _salonService.Id,
            _validDateStr,
            _validTimeStr);

        var act = () => _service.CreateAppointmentAsync(request);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*{nameof(StaffMember)}*");
    }

    [Fact]
    public async Task CreateAppointmentAsync_WhenStaffIsArchived_ThrowsNotFoundException()
    {
        var archivedStaff = StaffMember.Create(
            "پرسنل سابق",
            "old-staff",
            "09120000000",
            "",
            "Barber",
            2,
            serviceIds: new List<Guid> { _salonService.Id });
        archivedStaff.Archive();

        _staffRepository.GetByIdAsync(archivedStaff.Id, Arg.Any<CancellationToken>())
            .Returns(archivedStaff);

        var request = new CreateAppointmentRequest(
            _customer.Id,
            archivedStaff.Id,
            _salonService.Id,
            _validDateStr,
            _validTimeStr);

        var act = () => _service.CreateAppointmentAsync(request);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*{nameof(StaffMember)}*");
    }

    [Fact]
    public async Task CreateAppointmentAsync_WhenSalonServiceDoesNotExist_ThrowsNotFoundException()
    {
        var missingServiceId = Guid.NewGuid();
        _salonServiceRepository.GetByIdAsync(missingServiceId, Arg.Any<CancellationToken>())
            .Returns((SalonService?)null);

        var request = new CreateAppointmentRequest(
            _customer.Id,
            _staff.Id,
            missingServiceId,
            _validDateStr,
            _validTimeStr);

        var act = () => _service.CreateAppointmentAsync(request);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*{nameof(SalonService)}*");
    }

    [Fact]
    public async Task CreateAppointmentAsync_WhenSalonServiceIsArchived_ThrowsNotFoundException()
    {
        var archivedService = SalonService.Create("سرویس منسوخ", "", 30, 100000m, "Other");
        archivedService.Archive();

        _salonServiceRepository.GetByIdAsync(archivedService.Id, Arg.Any<CancellationToken>())
            .Returns(archivedService);

        var request = new CreateAppointmentRequest(
            _customer.Id,
            _staff.Id,
            archivedService.Id,
            _validDateStr,
            _validTimeStr);

        var act = () => _service.CreateAppointmentAsync(request);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*{nameof(SalonService)}*");
    }

    [Fact]
    public async Task CreateAppointmentAsync_WhenStaffDoesNotOfferRequestedService_ThrowsValidationException()
    {
        var otherService = SalonService.Create("کراتین", "تراپی مو", 60, 500000m, "Keratin");
        _salonServiceRepository.GetByIdAsync(otherService.Id, Arg.Any<CancellationToken>())
            .Returns(otherService);

        var request = new CreateAppointmentRequest(
            _customer.Id,
            _staff.Id,
            otherService.Id,
            _validDateStr,
            _validTimeStr);

        var act = () => _service.CreateAppointmentAsync(request);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*does not perform the selected service*");
    }

    [Fact]
    public async Task CreateAppointmentAsync_WhenSlotConflictsWithExistingActiveAppointment_ThrowsValidationException()
    {
        var date = DateOnly.Parse(_validDateStr);
        var conflictingSlot = TimeSlot.Create(date, new TimeOnly(10, 0), new TimeOnly(10, 30));
        var existingAppointment = Appointment.Create(
            Guid.NewGuid(),
            _staff.Id,
            _salonService.Id,
            conflictingSlot,
            150000m);

        _appointmentRepository.GetByStaffAndDateAsync(_staff.Id, date, Arg.Any<CancellationToken>())
            .Returns(new List<Appointment> { existingAppointment });

        var request = new CreateAppointmentRequest(
            _customer.Id,
            _staff.Id,
            _salonService.Id,
            _validDateStr,
            _validTimeStr);

        var act = () => _service.CreateAppointmentAsync(request);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*The selected time slot is already booked*");
    }

    [Fact]
    public async Task CreateAppointmentAsync_WhenExistingAppointmentAtSameSlotIsCancelled_CreatesAppointmentSuccessfully()
    {
        var date = DateOnly.Parse(_validDateStr);
        var cancelledSlot = TimeSlot.Create(date, new TimeOnly(10, 0), new TimeOnly(10, 30));
        var cancelledAppointment = Appointment.Create(
            Guid.NewGuid(),
            _staff.Id,
            _salonService.Id,
            cancelledSlot,
            150000m);
        cancelledAppointment.Cancel();

        _appointmentRepository.GetByStaffAndDateAsync(_staff.Id, date, Arg.Any<CancellationToken>())
            .Returns(new List<Appointment> { cancelledAppointment });

        var request = new CreateAppointmentRequest(
            _customer.Id,
            _staff.Id,
            _salonService.Id,
            _validDateStr,
            _validTimeStr);

        var response = await _service.CreateAppointmentAsync(request);

        response.Should().NotBeNull();
        response.Appointment.Status.Should().Be("pending");
    }

    [Fact]
    public async Task CreateAppointmentAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var request = new CreateAppointmentRequest(
            _customer.Id,
            _staff.Id,
            _salonService.Id,
            _validDateStr,
            _validTimeStr);

        var act = () => _service.CreateAppointmentAsync(request, cancellationToken: cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetByIdAsync_WhenAppointmentExists_ReturnsAppointmentDto()
    {
        var date = DateOnly.Parse(_validDateStr);
        var slot = TimeSlot.Create(date, new TimeOnly(10, 0), new TimeOnly(10, 30));
        var appointment = Appointment.Create(
            _customer.Id,
            _staff.Id,
            _salonService.Id,
            slot,
            150000m,
            "یادداشت نوبت");

        _appointmentRepository.GetByIdAsync(appointment.Id, Arg.Any<CancellationToken>())
            .Returns(appointment);

        var dto = await _service.GetByIdAsync(appointment.Id);

        dto.Should().NotBeNull();
        dto.Id.Should().Be(appointment.Id);
        dto.CustomerId.Should().Be(_customer.Id);
        dto.CustomerName.Should().Be(_customer.FullName);
        dto.StaffId.Should().Be(_staff.Id);
        dto.StaffName.Should().Be(_staff.FullName);
        dto.ServiceId.Should().Be(_salonService.Id);
        dto.ServiceName.Should().Be(_salonService.Name);
        dto.Date.Should().Be(_validDateStr);
        dto.Time.Should().Be("10:00");
        dto.Status.Should().Be("pending");
        dto.Price.Should().Be(150000m);
        dto.Notes.Should().Be("یادداشت نوبت");
    }

    [Fact]
    public async Task GetByIdAsync_WhenAppointmentDoesNotExist_ThrowsNotFoundException()
    {
        var missingId = Guid.NewGuid();
        _appointmentRepository.GetByIdAsync(missingId, Arg.Any<CancellationToken>())
            .Returns((Appointment?)null);

        var act = () => _service.GetByIdAsync(missingId);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*{nameof(Appointment)}*");
    }
}
