namespace BarberSalon.Application.Staff.DTOs;

/// <summary>
/// Input model for approving or rejecting a staff leave request.
/// </summary>
public sealed record ReviewLeaveRequest(
    bool Approved,
    string? AdminComment = null
);
