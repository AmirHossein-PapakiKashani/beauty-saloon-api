using BarberSalon.Domain.Common;
using BarberSalon.Domain.Staff.Enums;

namespace BarberSalon.Domain.Staff.Entities;

/// <summary>
/// A leave or absence request submitted by a staff member and reviewed by salon administration.
/// Acts as an independent Aggregate Root.
/// </summary>
public sealed class StaffLeaveRequest : BaseEntity
{
    /// <summary>Identifier of the staff member requesting leave.</summary>
    public Guid StaffMemberId { get; private set; }

    /// <summary>Whether the leave is full-day or hourly.</summary>
    public StaffLeaveType LeaveType { get; private set; }

    /// <summary>Start date and time of the requested absence.</summary>
    public DateTime StartDate { get; private set; }

    /// <summary>End date and time of the requested absence.</summary>
    public DateTime EndDate { get; private set; }

    /// <summary>Optional reason or notes provided by the staff member.</summary>
    public string Reason { get; private set; } = string.Empty;

    /// <summary>Current review status of the request.</summary>
    public StaffLeaveStatus Status { get; private set; }

    /// <summary>Optional comment left by the salon administrator upon approval or rejection.</summary>
    public string? AdminComment { get; private set; }

    /// <summary>Timestamp when the request was reviewed.</summary>
    public DateTime? ReviewedAt { get; private set; }

    /// <summary>Identifier of the administrator who reviewed the request.</summary>
    public Guid? ReviewedBy { get; private set; }

    // Private constructor required by EF Core
    private StaffLeaveRequest() { }

    /// <summary>
    /// Creates a new <see cref="StaffLeaveRequest"/> in Pending status.
    /// </summary>
    /// <param name="staffMemberId">Staff member identifier.</param>
    /// <param name="leaveType">Type of leave (Daily or Hourly).</param>
    /// <param name="startDate">Start timestamp.</param>
    /// <param name="endDate">End timestamp.</param>
    /// <param name="reason">Optional reason.</param>
    /// <returns>A new <see cref="StaffLeaveRequest"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public static StaffLeaveRequest Create(
        Guid staffMemberId,
        StaffLeaveType leaveType,
        DateTime startDate,
        DateTime endDate,
        string? reason = null)
    {
        if (staffMemberId == Guid.Empty)
            throw new DomainException("StaffMemberId is required.");

        if (endDate <= startDate)
            throw new DomainException("End date must be greater than start date.");

        return new StaffLeaveRequest
        {
            StaffMemberId = staffMemberId,
            LeaveType = leaveType,
            StartDate = startDate,
            EndDate = endDate,
            Reason = reason?.Trim() ?? string.Empty,
            Status = StaffLeaveStatus.Pending
        };
    }

    /// <summary>
    /// Approves the leave request.
    /// </summary>
    /// <param name="adminComment">Optional note from the administrator.</param>
    /// <param name="reviewedBy">Optional administrator ID.</param>
    /// <exception cref="DomainException">Thrown when not in Pending status.</exception>
    public void Approve(string? adminComment = null, Guid? reviewedBy = null)
    {
        if (Status != StaffLeaveStatus.Pending)
            throw new DomainException("Only pending leave requests can be reviewed.");

        Status = StaffLeaveStatus.Approved;
        AdminComment = adminComment?.Trim();
        ReviewedBy = reviewedBy;
        ReviewedAt = DateTime.UtcNow;
        Touch();
    }

    /// <summary>
    /// Rejects the leave request.
    /// </summary>
    /// <param name="adminComment">Optional note explaining the rejection.</param>
    /// <param name="reviewedBy">Optional administrator ID.</param>
    /// <exception cref="DomainException">Thrown when not in Pending status.</exception>
    public void Reject(string? adminComment = null, Guid? reviewedBy = null)
    {
        if (Status != StaffLeaveStatus.Pending)
            throw new DomainException("Only pending leave requests can be reviewed.");

        Status = StaffLeaveStatus.Rejected;
        AdminComment = adminComment?.Trim();
        ReviewedBy = reviewedBy;
        ReviewedAt = DateTime.UtcNow;
        Touch();
    }
}
