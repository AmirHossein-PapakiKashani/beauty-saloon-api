using BarberSalon.Domain.Common;
using BarberSalon.Domain.Staff.Entities;
using BarberSalon.Domain.Staff.Enums;
using FluentAssertions;
using Xunit;

namespace BarberSalon.Domain.Tests.Staff;

public class StaffLeaveRequestTests
{
    private readonly Guid _staffId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidDailyDates_InitializesAsPending()
    {
        // Arrange
        var start = DateTime.UtcNow.Date.AddDays(2);
        var end = start.AddDays(1);

        // Act
        var leave = StaffLeaveRequest.Create(
            _staffId,
            StaffLeaveType.Daily,
            start,
            end,
            "Family event");

        // Assert
        leave.Id.Should().NotBeEmpty();
        leave.StaffMemberId.Should().Be(_staffId);
        leave.LeaveType.Should().Be(StaffLeaveType.Daily);
        leave.StartDate.Should().Be(start);
        leave.EndDate.Should().Be(end);
        leave.Reason.Should().Be("Family event");
        leave.Status.Should().Be(StaffLeaveStatus.Pending);
        leave.AdminComment.Should().BeNull();
        leave.ReviewedAt.Should().BeNull();
    }

    [Fact]
    public void Create_WithEndDateBeforeStartDate_ThrowsDomainException()
    {
        // Arrange
        var start = DateTime.UtcNow.Date.AddDays(2);
        var end = start.AddDays(-1);

        // Act
        var act = () => StaffLeaveRequest.Create(_staffId, StaffLeaveType.Daily, start, end);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*End date must be greater than start date*");
    }

    [Fact]
    public void Create_WithEmptyStaffId_ThrowsDomainException()
    {
        // Act
        var act = () => StaffLeaveRequest.Create(Guid.Empty, StaffLeaveType.Daily, DateTime.UtcNow, DateTime.UtcNow.AddHours(2));

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*StaffMemberId is required*");
    }

    [Fact]
    public void Approve_WhenPending_SetsStatusToApproved()
    {
        // Arrange
        var start = DateTime.UtcNow.Date.AddDays(1);
        var leave = StaffLeaveRequest.Create(_staffId, StaffLeaveType.Hourly, start, start.AddHours(4), "Doctor visit");

        // Act
        leave.Approve("Approved by manager");

        // Assert
        leave.Status.Should().Be(StaffLeaveStatus.Approved);
        leave.AdminComment.Should().Be("Approved by manager");
        leave.ReviewedAt.Should().NotBeNull();
    }

    [Fact]
    public void Reject_WhenPending_SetsStatusToRejected()
    {
        // Arrange
        var start = DateTime.UtcNow.Date.AddDays(1);
        var leave = StaffLeaveRequest.Create(_staffId, StaffLeaveType.Daily, start, start.AddDays(1));

        // Act
        leave.Reject("Not enough shift coverage");

        // Assert
        leave.Status.Should().Be(StaffLeaveStatus.Rejected);
        leave.AdminComment.Should().Be("Not enough shift coverage");
        leave.ReviewedAt.Should().NotBeNull();
    }

    [Fact]
    public void Approve_WhenAlreadyApproved_ThrowsDomainException()
    {
        // Arrange
        var leave = StaffLeaveRequest.Create(_staffId, StaffLeaveType.Daily, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2));
        leave.Approve("First approval");

        // Act
        var act = () => leave.Approve("Second approval");

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Only pending leave requests can be reviewed*");
    }
}
