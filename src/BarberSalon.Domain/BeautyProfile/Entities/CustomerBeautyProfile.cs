using BarberSalon.Domain.Common;

namespace BarberSalon.Domain.BeautyProfile.Entities;

public sealed class CustomerBeautyProfile : BaseEntity
{
    private CustomerBeautyProfile() { }

    public Guid CustomerId { get; private set; }
    public string? HairType { get; private set; }
    public string CurrentHairColor { get; private set; } = string.Empty;
    public string Sensitivities { get; private set; } = string.Empty;
    public string Preferences { get; private set; } = string.Empty;
    public string Notes { get; private set; } = string.Empty;
    public string? SkinType { get; private set; }
    public string? Allergies { get; private set; }

    public static CustomerBeautyProfile Create(
        Guid customerId,
        string? hairType = null,
        string? currentHairColor = null,
        string? sensitivities = null,
        string? preferences = null,
        string? notes = null,
        string? skinType = null,
        string? allergies = null)
    {
        return new CustomerBeautyProfile
        {
            CustomerId = customerId,
            HairType = hairType?.Trim(),
            CurrentHairColor = currentHairColor?.Trim() ?? string.Empty,
            Sensitivities = sensitivities?.Trim() ?? string.Empty,
            Preferences = preferences?.Trim() ?? string.Empty,
            Notes = notes?.Trim() ?? string.Empty,
            SkinType = skinType?.Trim(),
            Allergies = allergies?.Trim()
        };
    }

    public void Update(
        string? hairType,
        string? currentHairColor,
        string? sensitivities,
        string? preferences,
        string? notes,
        string? skinType = null,
        string? allergies = null)
    {
        HairType = hairType?.Trim();
        CurrentHairColor = currentHairColor?.Trim() ?? string.Empty;
        Sensitivities = sensitivities?.Trim() ?? string.Empty;
        Preferences = preferences?.Trim() ?? string.Empty;
        Notes = notes?.Trim() ?? string.Empty;
        SkinType = skinType?.Trim();
        Allergies = allergies?.Trim();
        Touch();
    }
}
