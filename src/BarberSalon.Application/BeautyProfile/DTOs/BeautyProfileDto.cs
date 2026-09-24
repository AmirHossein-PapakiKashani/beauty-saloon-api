namespace BarberSalon.Application.BeautyProfile.DTOs;

public sealed record BeautyProfileDto(
    Guid CustomerId,
    string? HairType,
    string CurrentHairColor,
    string Sensitivities,
    string Preferences,
    string Notes,
    string? SkinType,
    string? Allergies,
    DateTimeOffset UpdatedAt
);

public sealed record UpdateBeautyProfileRequest(
    string? HairType,
    string? CurrentHairColor,
    string? Sensitivities,
    string? Preferences,
    string? Notes,
    string? SkinType = null,
    string? Allergies = null
);
