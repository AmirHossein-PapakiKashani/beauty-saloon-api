namespace BarberSalon.Application.Staff.DTOs;

/// <summary>
/// Read model returned to clients representing a staff member.
/// Maps directly to the frontend Staff type contract.
/// </summary>
public record StaffDto(
    Guid Id,
    string Slug,
    string Name,
    string Phone,
    string Bio,
    string Role,
    bool IsActive,
    List<Guid> Services,
    int TodayAppointments,
    int YearsExperience,
    List<string> Specialties,
    string WorkingHoursJson,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
