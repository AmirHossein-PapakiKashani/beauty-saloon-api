namespace BarberSalon.Application.SalonServices.DTOs;

/// <summary>Read model returned to the client for a salon service.</summary>
public record SalonServiceDto(
    Guid Id,
    string Name,
    string Description,
    int DurationMinutes,
    decimal Price,
    string Category,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    decimal? MinPrice = null,
    decimal? MaxPrice = null
);
