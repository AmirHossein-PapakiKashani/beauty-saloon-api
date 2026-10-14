namespace BarberSalon.Application.Staff.DTOs;

/// <summary>
/// Read model representing a staff leave or absence request.
/// </summary>
public sealed record StaffLeaveDto(
    Guid Id,
    Guid StaffMemberId,
    string StaffMemberName,
    string LeaveType,
    DateTime StartDate,
    DateTime EndDate,
    string Reason,
    string Status,
    string? AdminComment,
    DateTime? ReviewedAt,
    Guid? ReviewedBy,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
