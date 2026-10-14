using BarberSalon.Application.Auth.Interfaces;
using BarberSalon.Application.Booking.Interfaces;
using BarberSalon.Application.Common.Interfaces;
using BarberSalon.Application.SalonServices.Interfaces;
using BarberSalon.Application.Staff.DTOs;
using BarberSalon.Application.Staff.Interfaces;
using BarberSalon.Application.Staff.Services;
using BarberSalon.Domain.Auth.Entities;
using BarberSalon.Domain.Auth.Enums;
using BarberSalon.Domain.Booking.Entities;
using BarberSalon.Domain.Booking.ValueObjects;
using BarberSalon.Domain.SalonServices.Entities;
using BarberSalon.Domain.Staff.Entities;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace BarberSalon.Application.Tests.Staff;

public class StaffEarningsTests
{
    private readonly IStaffRepository _staffRepo;
    private readonly IAppointmentRepository _appointmentRepo;
    private readonly ISalonServiceRepository _salonServiceRepo;
    private readonly IUserRepository _userRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly StaffService _sut;

    public StaffEarningsTests()
    {
        _staffRepo = Substitute.For<IStaffRepository>();
        _appointmentRepo = Substitute.For<IAppointmentRepository>();
        _salonServiceRepo = Substitute.For<ISalonServiceRepository>();
        _userRepo = Substitute.For<IUserRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        _sut = new StaffService(
            _staffRepo,
            _unitOfWork,
            _appointmentRepo,
            _salonServiceRepo,
            _userRepo);
    }

    [Fact]
    public async Task GetStaffEarningsAsync_WithCompletedAppointments_ComputesRevenueAndCommissionsCorrectly()
    {
        // Arrange
        var staff = StaffMember.Create("آقای کریمی", "karimi", "09121111111", "بیوگرافی", "آرایشگر ارشد", 10);
        var staffId = staff.Id;
        var s1Id = Guid.NewGuid();
        var s2Id = Guid.NewGuid();
        staff.SetCommissionRates(40m, new Dictionary<Guid, decimal> { [s2Id] = 50m });

        _staffRepo.GetByIdAsync(staffId, Arg.Any<CancellationToken>()).Returns(staff);

        var customerId = Guid.NewGuid();
        var customer = User.Create("09121234567", DateTime.UtcNow, UserRole.Customer, "علی احمدی");
        _userRepo.GetByIdAsync(customerId, Arg.Any<CancellationToken>()).Returns(customer);

        var service1 = SalonService.Create("کوتاهی مو", "کوتاهی ساده", 45, 1_000_000m, "haircut");
        var service2 = SalonService.Create("رنگ مو", "رنگ تخصصی", 90, 600_000m, "color");
        _salonServiceRepo.GetByIdAsync(s1Id, Arg.Any<CancellationToken>()).Returns(service1);
        _salonServiceRepo.GetByIdAsync(s2Id, Arg.Any<CancellationToken>()).Returns(service2);

        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var slot1 = TimeSlot.Create(date, new TimeOnly(10, 0), new TimeOnly(11, 0));
        var appt1 = Appointment.Create(customerId, staffId, s1Id, slot1, 1_000_000m);
        appt1.Confirm();
        appt1.StartService();
        appt1.Complete(commissionRate: 40m); // CommissionEarned = 400,000

        var slot2 = TimeSlot.Create(date, new TimeOnly(11, 30), new TimeOnly(13, 0));
        var appt2 = Appointment.Create(customerId, staffId, s2Id, slot2, 600_000m);
        appt2.Confirm();
        appt2.StartService();
        appt2.Complete(commissionRate: 50m); // CommissionEarned = 300,000

        var slot3 = TimeSlot.Create(date, new TimeOnly(14, 0), new TimeOnly(15, 0));
        var apptCancelled = Appointment.Create(customerId, staffId, s1Id, slot3, 1_000_000m);
        apptCancelled.Cancel("کنسل توسط مشتری");

        _appointmentRepo.GetByStaffAndDateRangeAsync(staffId, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new List<Appointment> { appt1, appt2, apptCancelled });

        // Act
        var result = await _sut.GetStaffEarningsAsync(staffId);

        // Assert
        result.Should().NotBeNull();
        result.StaffId.Should().Be(staffId);
        result.StaffName.Should().Be("آقای کریمی");
        result.TotalAppointmentsCompleted.Should().Be(2);
        result.TotalServiceRevenue.Should().Be(1_600_000m);
        result.TotalCommissionEarned.Should().Be(700_000m);
        result.DefaultCommissionRate.Should().Be(40m);
        result.Items.Should().HaveCount(2);

        var item1 = result.Items.First(i => i.AppointmentId == appt1.Id);
        item1.ServicePrice.Should().Be(1_000_000m);
        item1.CommissionRate.Should().Be(40m);
        item1.CommissionEarned.Should().Be(400_000m);

        var item2 = result.Items.First(i => i.AppointmentId == appt2.Id);
        item2.ServicePrice.Should().Be(600_000m);
        item2.CommissionRate.Should().Be(50m);
        item2.CommissionEarned.Should().Be(300_000m);
    }

    [Fact]
    public async Task UpdateSelfProfileAsync_UpdatesBioAndAvatar_PreservesRoleAndRates()
    {
        // Arrange
        var staff = StaffMember.Create("آقای کریمی", "karimi", "09121111111", "بیوگرافی قدیمی", "آرایشگر ارشد", 10);
        var staffId = staff.Id;
        staff.SetCommissionRates(40m);

        _staffRepo.GetByIdAsync(staffId, Arg.Any<CancellationToken>()).Returns(staff);

        var request = new UpdateStaffSelfProfileRequest(
            Bio: "بیوگرافی جدید ویرایش شده توسط پرسنل",
            AvatarUrl: "https://example.com/new-avatar.jpg"
        );

        // Act
        var result = await _sut.UpdateSelfProfileAsync(staffId, request);

        // Assert
        result.Bio.Should().Be("بیوگرافی جدید ویرایش شده توسط پرسنل");
        result.AvatarUrl.Should().Be("https://example.com/new-avatar.jpg");
        result.Role.Should().Be("آرایشگر ارشد");
        result.DefaultCommissionRate.Should().Be(40m);

        _staffRepo.Received(1).Update(staff);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
