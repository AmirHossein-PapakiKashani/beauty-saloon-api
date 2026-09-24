namespace BarberSalon.Application.SalonServices.DTOs;

/// <summary>Input model for updating an existing salon service.</summary>
public record UpdateSalonServiceRequest(
    string Name,
    string Description,
    int DurationMinutes,
    decimal Price,
    string Category,
    bool? IsActive = null
);
