using BarberSalon.Application.Common.Exceptions;
using BarberSalon.Application.Common.Interfaces;
using BarberSalon.Application.Staff.DTOs;
using BarberSalon.Application.Staff.Interfaces;
using BarberSalon.Application.Staff.Services;
using BarberSalon.Domain.Common;
using BarberSalon.Domain.Staff.Entities;
using BarberSalon.Domain.Staff.Enums;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace BarberSalon.Application.Tests.Staff;

public class StaffLeaveServiceTests
{
    private readonly IStaffLeaveRequestRepository _leaveRepo;
    private readonly IStaffRepository _staffRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly StaffLeaveService _sut;

    public StaffLeaveServiceTests()
    {
        _leaveRepo = Substitute.For<IStaffLeaveRequestRepository>();
        _staffRepo = Substitute.For<IStaffRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        _sut = new StaffLeaveService(_leaveRepo, _staffRepo, _unitOfWork);
    }

    [Fact]
    public async Task SubmitLeaveAsync_WithValidDailyRequest_CreatesAndReturnsPendingDto()
    {
        // Arrange
        var staffId = Guid.NewGuid();
        var staff = StaffMember.Create("رضا رضایی", "reza", "09121234567", "", "آرایشگر", 5);
        _staffRepo.GetByIdAsync(staffId, Arg.Any<CancellationToken>()).Returns(staff);

        var request = new SubmitLeaveRequest(
            LeaveType: "daily",
            StartDate: DateTime.UtcNow.AddDays(1),
            EndDate: DateTime.UtcNow.AddDays(3),
            Reason: "سفر خانوادگی"
        );

        // Act
        var result = await _sut.SubmitLeaveAsync(staffId, request);

        // Assert
        result.Should().NotBeNull();
        result.StaffMemberName.Should().Be("رضا رضایی");
        result.LeaveType.Should().Be("daily");
        result.Status.Should().Be("pending");
        result.Reason.Should().Be("سفر خانوادگی");

        await _leaveRepo.Received(1).AddAsync(Arg.Is<StaffLeaveRequest>(l =>
            l.StaffMemberId == staffId &&
            l.LeaveType == StaffLeaveType.Daily &&
            l.Status == StaffLeaveStatus.Pending),
            Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitLeaveAsync_WithNonExistentStaff_ThrowsNotFoundException()
    {
        // Arrange
        var staffId = Guid.NewGuid();
        _staffRepo.GetByIdAsync(staffId, Arg.Any<CancellationToken>()).Returns((StaffMember?)null);

        var request = new SubmitLeaveRequest("daily", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2));

        // Act
        var act = () => _sut.SubmitLeaveAsync(staffId, request);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task SubmitLeaveAsync_WithInvalidLeaveType_ThrowsDomainException()
    {
        // Arrange
        var staffId = Guid.NewGuid();
        var staff = StaffMember.Create("رضا رضایی", "reza", "09121234567", "", "آرایشگر", 5);
        _staffRepo.GetByIdAsync(staffId, Arg.Any<CancellationToken>()).Returns(staff);

        var request = new SubmitLeaveRequest("monthly", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2));

        // Act
        var act = () => _sut.SubmitLeaveAsync(staffId, request);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*Invalid leave type*");
    }

    [Fact]
    public async Task ReviewLeaveAsync_WhenApproved_UpdatesStatusAndAdminComment()
    {
        // Arrange
        var leaveId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var staff = StaffMember.Create("علی کریمی", "karimi", "09121111111", "", "آرایشگر", 8);
        var leave = StaffLeaveRequest.Create(staffId, StaffLeaveType.Daily, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2));

        _leaveRepo.GetByIdAsync(leaveId, Arg.Any<CancellationToken>()).Returns(leave);
        _staffRepo.GetByIdAsync(staffId, Arg.Any<CancellationToken>()).Returns(staff);

        var adminId = Guid.NewGuid();
        var review = new ReviewLeaveRequest(Approved: true, AdminComment: "تایید شد، موفق باشید");

        // Act
        var result = await _sut.ReviewLeaveAsync(leaveId, review, adminId);

        // Assert
        result.Status.Should().Be("approved");
        result.AdminComment.Should().Be("تایید شد، موفق باشید");
        result.ReviewedBy.Should().Be(adminId);

        _leaveRepo.Received(1).Update(leave);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReviewLeaveAsync_WhenRejected_UpdatesStatusToRejected()
    {
        // Arrange
        var leaveId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var staff = StaffMember.Create("علی کریمی", "karimi", "09121111111", "", "آرایشگر", 8);
        var leave = StaffLeaveRequest.Create(staffId, StaffLeaveType.Daily, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2));

        _leaveRepo.GetByIdAsync(leaveId, Arg.Any<CancellationToken>()).Returns(leave);
        _staffRepo.GetByIdAsync(staffId, Arg.Any<CancellationToken>()).Returns(staff);

        var adminId = Guid.NewGuid();
        var review = new ReviewLeaveRequest(Approved: false, AdminComment: "همپوشانی با شیفت ضروری سالن");

        // Act
        var result = await _sut.ReviewLeaveAsync(leaveId, review, adminId);

        // Assert
        result.Status.Should().Be("rejected");
        result.AdminComment.Should().Be("همپوشانی با شیفت ضروری سالن");

        _leaveRepo.Received(1).Update(leave);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
