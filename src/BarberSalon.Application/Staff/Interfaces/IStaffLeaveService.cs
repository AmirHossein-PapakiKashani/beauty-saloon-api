using BarberSalon.Application.Staff.DTOs;

namespace BarberSalon.Application.Staff.Interfaces;

/// <summary>
/// Service contract for managing staff leave and absence requests.
/// </summary>
public interface IStaffLeaveService
{
    /// <summary>Submits a new leave request for a staff member.</summary>
    Task<StaffLeaveDto> SubmitLeaveAsync(Guid staffMemberId, SubmitLeaveRequest request, CancellationToken cancellationToken = default);

    /// <summary>Gets all leave requests submitted by a specific staff member.</summary>
    Task<List<StaffLeaveDto>> GetStaffLeavesAsync(Guid staffMemberId, CancellationToken cancellationToken = default);

    /// <summary>Gets all pending leave requests across all staff members for administrator review.</summary>
    Task<List<StaffLeaveDto>> GetPendingLeavesAsync(CancellationToken cancellationToken = default);

    /// <summary>Reviews (approves or rejects) a staff leave request.</summary>
    Task<StaffLeaveDto> ReviewLeaveAsync(Guid leaveId, ReviewLeaveRequest request, Guid adminUserId, CancellationToken cancellationToken = default);
}
