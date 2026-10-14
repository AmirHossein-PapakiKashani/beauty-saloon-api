namespace BarberSalon.Application.Staff.DTOs;

/// <summary>
/// Input model for submitting a staff leave request.
/// </summary>
public sealed record SubmitLeaveRequest(
    string LeaveType,
    DateTime StartDate,
    DateTime EndDate,
    string? Reason = null
);
