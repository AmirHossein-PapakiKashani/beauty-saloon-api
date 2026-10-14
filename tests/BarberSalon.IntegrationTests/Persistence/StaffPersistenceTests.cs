using BarberSalon.Application.Staff.Interfaces;
using BarberSalon.Domain.Booking.Entities;
using BarberSalon.Domain.Booking.Enums;
using BarberSalon.Domain.Booking.ValueObjects;
using BarberSalon.Domain.Staff.Entities;
using BarberSalon.Domain.Staff.Enums;
using BarberSalon.Infrastructure.Persistence;
using BarberSalon.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BarberSalon.IntegrationTests.Persistence;

public class StaffPersistenceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly IStaffRepository _staffRepository;
    private readonly IStaffLeaveRequestRepository _leaveRequestRepository;

    public StaffPersistenceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "StaffPersistenceDb_" + Guid.NewGuid())
            .Options;

        _context = new AppDbContext(options);
        _staffRepository = new StaffRepository(_context);
        _leaveRequestRepository = new StaffLeaveRequestRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task StaffMember_WithCommissionsAndAvatar_PersistsAndCanBeRetrievedByUserIdAndPhone()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var staff = StaffMember.Create(
            fullName: "استاد علی",
            slug: "ali-master",
            phoneNumber: "09129998877",
            bio: "استاد حرفه‌ای",
            role: "آرایشگر ارشد",
            yearsExperience: 8,
            userId: userId
        );
        staff.UpdateSelfProfile(bio: "استاد حرفه‌ای ویرایش شده", avatarUrl: "https://example.com/avatar.jpg");
        staff.SetCommissionRates(35m, new Dictionary<Guid, decimal> { [serviceId] = 45m });

        // Act
        await _staffRepository.AddAsync(staff);
        await _context.SaveChangesAsync();

        var retrievedById = await _staffRepository.GetByIdAsync(staff.Id);
        var retrievedByUserId = await _staffRepository.GetByUserIdAsync(userId);
        var retrievedByPhone = await _staffRepository.GetByPhoneNumberAsync("09129998877");

        // Assert
        retrievedById.Should().NotBeNull();
        retrievedById!.AvatarUrl.Should().Be("https://example.com/avatar.jpg");
        retrievedById.DefaultCommissionRate.Should().Be(35m);
        retrievedById.ServiceCommissionRates.Should().ContainKey(serviceId).WhoseValue.Should().Be(45m);

        retrievedByUserId.Should().NotBeNull();
        retrievedByUserId!.Id.Should().Be(staff.Id);

        retrievedByPhone.Should().NotBeNull();
        retrievedByPhone!.Id.Should().Be(staff.Id);
    }

    [Fact]
    public async Task Appointment_WithCommissionAndDelay_PersistsCorrectly()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var slot = TimeSlot.Create(date, new TimeOnly(10, 0), new TimeOnly(11, 0));
        var appointment = Appointment.Create(
            customerId: Guid.NewGuid(),
            staffId: Guid.NewGuid(),
            salonServiceId: Guid.NewGuid(),
            timeSlot: slot,
            price: 200_000m
        );
        appointment.Confirm();
        appointment.StartService();
        appointment.RecordDelay(15);
        appointment.Complete(commissionRate: 30m);

        // Act
        await _context.Appointments.AddAsync(appointment);
        await _context.SaveChangesAsync();

        var retrieved = await _context.Appointments.FirstOrDefaultAsync(a => a.Id == appointment.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Status.Should().Be(AppointmentStatus.Completed);
        retrieved.DelayMinutes.Should().Be(15);
        retrieved.CommissionRate.Should().Be(30m);
        retrieved.CommissionEarned.Should().Be(60_000m);
    }

    [Fact]
    public async Task StaffLeaveRequest_RepositoryOperations_PerformExpectedFiltering()
    {
        // Arrange
        var staffId = Guid.NewGuid();
        var otherStaffId = Guid.NewGuid();

        var leave1 = StaffLeaveRequest.Create(
            staffMemberId: staffId,
            leaveType: StaffLeaveType.Daily,
            startDate: DateTime.UtcNow.AddDays(2),
            endDate: DateTime.UtcNow.AddDays(4),
            reason: "مرخصی استعلاجی"
        );

        var leave2 = StaffLeaveRequest.Create(
            staffMemberId: staffId,
            leaveType: StaffLeaveType.Hourly,
            startDate: DateTime.UtcNow.AddDays(5).Date.AddHours(10),
            endDate: DateTime.UtcNow.AddDays(5).Date.AddHours(14),
            reason: "امور شخصی"
        );
        leave2.Approve(adminComment: "تایید شد");

        var leave3 = StaffLeaveRequest.Create(
            staffMemberId: otherStaffId,
            leaveType: StaffLeaveType.Daily,
            startDate: DateTime.UtcNow.AddDays(1),
            endDate: DateTime.UtcNow.AddDays(2)
        );

        await _leaveRequestRepository.AddAsync(leave1);
        await _leaveRequestRepository.AddAsync(leave2);
        await _leaveRequestRepository.AddAsync(leave3);
        await _context.SaveChangesAsync();

        // Act & Assert
        // 1. GetByIdAsync
        var byId = await _leaveRequestRepository.GetByIdAsync(leave1.Id);
        byId.Should().NotBeNull();
        byId!.Reason.Should().Be("مرخصی استعلاجی");
        byId.Status.Should().Be(StaffLeaveStatus.Pending);

        // 2. GetByStaffIdAsync
        var staffLeaves = await _leaveRequestRepository.GetByStaffIdAsync(staffId);
        staffLeaves.Should().HaveCount(2);
        staffLeaves.Select(l => l.Id).Should().Contain(new[] { leave1.Id, leave2.Id });

        // 3. GetPendingAsync
        var pending = await _leaveRequestRepository.GetPendingAsync();
        pending.Should().HaveCount(2);
        pending.Select(l => l.Id).Should().Contain(new[] { leave1.Id, leave3.Id });

        // 4. GetApprovedByStaffAndDateRangeAsync
        var overlapping = await _leaveRequestRepository.GetApprovedByStaffAndDateRangeAsync(
            staffId,
            startDate: DateTime.UtcNow.AddDays(5).Date,
            endDate: DateTime.UtcNow.AddDays(6).Date
        );
        overlapping.Should().HaveCount(1);
        overlapping.First().Id.Should().Be(leave2.Id);

        // 5. Update: Approve leave1
        leave1.Approve(adminComment: "موافقت شد");
        _leaveRequestRepository.Update(leave1);
        await _context.SaveChangesAsync();

        var updatedPending = await _leaveRequestRepository.GetPendingAsync();
        updatedPending.Should().HaveCount(1);
        updatedPending.First().Id.Should().Be(leave3.Id);
    }
}
