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
using BarberSalon.Domain.Booking.Enums;
using BarberSalon.Domain.Booking.ValueObjects;
using BarberSalon.Domain.SalonServices.Entities;
using BarberSalon.Domain.Staff.Entities;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace BarberSalon.Application.Tests.Booking;

public class GetAppointmentsByDateTests
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IStaffRepository _staffRepository;
    private readonly ISalonServiceRepository _salonServiceRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly BookingService _sut;

    public GetAppointmentsByDateTests()
    {
        _appointmentRepository = Substitute.For<IAppointmentRepository>();
        _staffRepository = Substitute.For<IStaffRepository>();
        _salonServiceRepository = Substitute.For<ISalonServiceRepository>();
        _userRepository = Substitute.For<IUserRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _clock = Substitute.For<IClock>();

        _clock.UtcNow.Returns(new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc));

        _sut = new BookingService(
            _appointmentRepository,
            _staffRepository,
            _salonServiceRepository,
            _userRepository,
            _unitOfWork,
            _clock);
    }

    [Fact]
    public async Task GetAppointmentsByDateAsync_WhenAppointmentsExistForDate_ReturnsMappedAppointmentDtos()
    {
        // Arrange
        var targetDate = new DateOnly(2026, 9, 1);
        var targetDateString = "2026-09-01";

        var customer = User.Create("09121112233", DateTime.UtcNow, UserRole.Customer, "علی رضایی");
        var staff = StaffMember.Create("سارا محمدی", "sara-mohammadi", "09123334455", "استایلیست", "آرایشگر ارشد", 5);
        var service = SalonService.Create("کوتاهی مو", "کوتاهی حرفه‌ای", 45, 150000m, "haircut");

        var timeSlot = TimeSlot.Create(targetDate, new TimeOnly(10, 0), new TimeOnly(10, 45));
        var appointment = Appointment.Create(customer.Id, staff.Id, service.Id, timeSlot, 150000m, "تست یادداشت");

        _appointmentRepository.GetByDateAsync(targetDate, Arg.Any<CancellationToken>())
            .Returns(new List<Appointment> { appointment });

        _userRepository.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>())
            .Returns(customer);

        _staffRepository.GetByIdAsync(staff.Id, Arg.Any<CancellationToken>())
            .Returns(staff);

        _salonServiceRepository.GetByIdAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns(service);

        // Act
        var result = await _sut.GetAppointmentsByDateAsync(targetDateString);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);

        var dto = result[0];
        dto.Id.Should().Be(appointment.Id);
        dto.CustomerId.Should().Be(customer.Id);
        dto.CustomerName.Should().Be("علی رضایی");
        dto.StaffId.Should().Be(staff.Id);
        dto.StaffName.Should().Be("سارا محمدی");
        dto.ServiceId.Should().Be(service.Id);
        dto.ServiceName.Should().Be("کوتاهی مو");
        dto.Date.Should().Be("2026-09-01");
        dto.Time.Should().Be("10:00");
        dto.Status.Should().Be("pending");
        dto.Price.Should().Be(150000m);
        dto.Notes.Should().Be("تست یادداشت");
    }

    [Fact]
    public async Task GetAppointmentsByDateAsync_WhenNoAppointmentsForDate_ReturnsEmptyList()
    {
        // Arrange
        var targetDate = new DateOnly(2026, 9, 2);
        _appointmentRepository.GetByDateAsync(targetDate, Arg.Any<CancellationToken>())
            .Returns(new List<Appointment>());

        // Act
        var result = await _sut.GetAppointmentsByDateAsync("2026-09-02");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task GetAppointmentsByDateAsync_WhenDateStringIsEmptyOrWhitespace_ThrowsValidationException(string? invalidDate)
    {
        // Act
        var act = () => _sut.GetAppointmentsByDateAsync(invalidDate!);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Date cannot be empty*");
    }

    [Theory]
    [InlineData("2026/09/01")]
    [InlineData("01-09-2026")]
    [InlineData("invalid-date")]
    [InlineData("2026-13-40")]
    public async Task GetAppointmentsByDateAsync_WhenDateFormatIsInvalid_ThrowsValidationException(string invalidDate)
    {
        // Act
        var act = () => _sut.GetAppointmentsByDateAsync(invalidDate);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Invalid date format*");
    }

    [Fact]
    public async Task GetAppointmentsByDateAsync_WhenRelatedEntitiesMissing_FallsBackToDefaultNames()
    {
        // Arrange
        var targetDate = new DateOnly(2026, 9, 1);
        var customerId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();

        var timeSlot = TimeSlot.Create(targetDate, new TimeOnly(14, 0), new TimeOnly(14, 30));
        var appointment = Appointment.Create(customerId, staffId, serviceId, timeSlot, 100000m);

        _appointmentRepository.GetByDateAsync(targetDate, Arg.Any<CancellationToken>())
            .Returns(new List<Appointment> { appointment });

        _userRepository.GetByIdAsync(customerId, Arg.Any<CancellationToken>()).Returns((User?)null);
        _staffRepository.GetByIdAsync(staffId, Arg.Any<CancellationToken>()).Returns((StaffMember?)null);
        _salonServiceRepository.GetByIdAsync(serviceId, Arg.Any<CancellationToken>()).Returns((SalonService?)null);

        // Act
        var result = await _sut.GetAppointmentsByDateAsync(targetDate);

        // Assert
        result.Should().HaveCount(1);
        result[0].CustomerName.Should().Be("مشتری");
        result[0].StaffName.Should().Be("پرسنل");
        result[0].ServiceName.Should().Be("سرویس");
    }

    [Fact]
    public async Task GetAppointmentsByDateAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var targetDate = new DateOnly(2026, 9, 1);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = () => _sut.GetAppointmentsByDateAsync(targetDate, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
