using BarberSalon.Domain.Staff.Entities;

namespace BarberSalon.Application.Staff.Interfaces;

/// <summary>
/// Data access contract for <see cref="StaffLeaveRequest"/> entities.
/// </summary>
public interface IStaffLeaveRequestRepository
{
    /// <summary>Returns a leave request by its unique identifier, or null if not found.</summary>
    Task<StaffLeaveRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns all leave requests submitted by a specific staff member ordered by start date descending.</summary>
    Task<List<StaffLeaveRequest>> GetByStaffIdAsync(Guid staffMemberId, CancellationToken cancellationToken = default);

    /// <summary>Returns all pending leave requests across all staff members (for admin review).</summary>
    Task<List<StaffLeaveRequest>> GetPendingAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns approved leave requests for a staff member overlapping a given date range.</summary>
    Task<List<StaffLeaveRequest>> GetApprovedByStaffAndDateRangeAsync(
        Guid staffMemberId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>Adds a new leave request to the EF Core change tracker.</summary>
    Task AddAsync(StaffLeaveRequest request, CancellationToken cancellationToken = default);

    /// <summary>Marks a modified leave request as updated in the EF Core change tracker.</summary>
    void Update(StaffLeaveRequest request);
}
