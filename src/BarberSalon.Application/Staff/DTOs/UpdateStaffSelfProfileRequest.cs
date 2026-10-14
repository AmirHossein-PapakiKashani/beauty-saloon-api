namespace BarberSalon.Application.Staff.DTOs;

/// <summary>
/// Input model for a staff member self-updating their bio and avatar.
/// </summary>
public sealed record UpdateStaffSelfProfileRequest(
    string? Bio,
    string? AvatarUrl
);
