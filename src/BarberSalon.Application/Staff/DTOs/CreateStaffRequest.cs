namespace BarberSalon.Application.Staff.DTOs;

/// <summary>
/// Input model for creating a new staff member.
/// </summary>
public record CreateStaffRequest(
    string Name,
    string Slug,
    string Phone,
    string Bio,
    string Role,
    int YearsExperience,
    List<string>? Specialties = null,
    List<Guid>? Services = null,
    Guid? UserId = null,
    string? WorkingHoursJson = null
);
