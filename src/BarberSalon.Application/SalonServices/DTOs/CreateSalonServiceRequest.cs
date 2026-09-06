namespace BarberSalon.Application.SalonServices.DTOs;

/// <summary>Input model for creating a new salon service.</summary>
public record CreateSalonServiceRequest(
    string Name,
    string Description,
    int DurationMinutes,
    decimal Price,
    string Category
);
