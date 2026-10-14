namespace BarberSalon.Domain.Staff.Enums;

/// <summary>
/// The review status of a staff leave request.
/// </summary>
public enum StaffLeaveStatus
{
    /// <summary>Submitted by staff member, awaiting salon admin review.</summary>
    Pending = 1,

    /// <summary>Approved by salon admin, blocking staff availability slots.</summary>
    Approved = 2,

    /// <summary>Rejected by salon admin.</summary>
    Rejected = 3
}
