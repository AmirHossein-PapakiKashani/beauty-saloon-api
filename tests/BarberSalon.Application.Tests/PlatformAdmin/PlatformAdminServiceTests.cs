using BarberSalon.Application.Auth.Interfaces;
using BarberSalon.Application.Booking.Interfaces;
using BarberSalon.Application.Common.Interfaces;
using BarberSalon.Application.PlatformAdmin.DTOs;
using BarberSalon.Application.PlatformAdmin.Services;
using BarberSalon.Application.Staff.Interfaces;
using BarberSalon.Domain.Auth.Entities;
using BarberSalon.Domain.Auth.Enums;
using BarberSalon.Domain.Booking.Entities;
using BarberSalon.Domain.Salons.Entities;
using BarberSalon.Domain.Salons.Repositories;
using BarberSalon.Domain.Staff.Entities;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace BarberSalon.Application.Tests.PlatformAdmin;

public class PlatformAdminServiceTests
{
    private readonly ISalonRepository _salonRepo;
    private readonly IUserRepository _userRepo;
    private readonly IStaffRepository _staffRepo;
    private readonly IAppointmentRepository _appointmentRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public PlatformAdminServiceTests()
    {
        _salonRepo = Substitute.For<ISalonRepository>();
        _userRepo = Substitute.For<IUserRepository>();
        _staffRepo = Substitute.For<IStaffRepository>();
        _appointmentRepo = Substitute.For<IAppointmentRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _clock = Substitute.For<IClock>();
        _clock.UtcNow.Returns(new DateTime(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc));
    }

    private PlatformAdminService CreateService() =>
        new(_salonRepo, _userRepo, _staffRepo, _appointmentRepo, _unitOfWork, _clock);

    [Fact]
    public async Task GetMetricsAsync_ShouldReturnAggregatedMetrics()
    {
        _salonRepo.CountAsync(null, Arg.Any<CancellationToken>()).Returns(5);
        _salonRepo.CountAsync(true, Arg.Any<CancellationToken>()).Returns(4);
        _staffRepo.GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(new List<StaffMember> {
                StaffMember.Create("Stylist One", "09121110001", "stylist-one", "Senior Stylist", 5)
            });

        var userList = new List<User>
        {
            User.Create("09121110001", DateTime.UtcNow.AddDays(-10), UserRole.Customer),
            User.Create("09121110002", DateTime.UtcNow.AddDays(-5), UserRole.Staff)
        };
        userList[0].TouchActive(DateTime.UtcNow.AddHours(-2)); // Active in 24h
        _userRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(userList);

        _appointmentRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Appointment>());

        var service = CreateService();
        var metrics = await service.GetMetricsAsync();

        metrics.TotalSalons.Should().Be(5);
        metrics.ActiveSalonsCount.Should().Be(4);
        metrics.TotalUsers.Should().Be(2);
        metrics.ActiveUsersCount24h.Should().Be(1);
        metrics.ActiveStaffCount.Should().Be(1);
        metrics.SystemStatus.Should().Be("Healthy");
    }

    [Fact]
    public async Task CreateSalonAsync_WithValidRequest_ShouldCallRepositoryAdd()
    {
        _salonRepo.SlugExistsAsync("new-salon", null, Arg.Any<CancellationToken>()).Returns(false);
        var service = CreateService();

        var request = new CreatePlatformSalonRequest(
            "New Salon", "new-salon", "0217777", "Tehran Address", "Desc", null);

        var result = await service.CreateSalonAsync(request);

        result.Name.Should().Be("New Salon");
        result.Slug.Should().Be("new-salon");
        await _salonRepo.Received(1).AddAsync(Arg.Any<Salon>(), Arg.Any<CancellationToken>());
    }
}
