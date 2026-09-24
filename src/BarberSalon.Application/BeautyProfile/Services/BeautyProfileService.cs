using BarberSalon.Application.BeautyProfile.DTOs;
using BarberSalon.Application.BeautyProfile.Interfaces;
using BarberSalon.Application.Common.Exceptions;
using BarberSalon.Application.Common.Interfaces;
using BarberSalon.Domain.BeautyProfile.Entities;

namespace BarberSalon.Application.BeautyProfile.Services;

public sealed class BeautyProfileService(
    IBeautyProfileRepository repository,
    IUnitOfWork unitOfWork)
{
    public async Task<BeautyProfileDto> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default)
    {
        var profile = await repository.GetByCustomerIdAsync(customerId, ct);
        if (profile == null)
        {
            return new BeautyProfileDto(
                customerId,
                null,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                null,
                null,
                DateTimeOffset.UtcNow
            );
        }

        return MapToDto(profile);
    }

    public async Task<BeautyProfileDto> UpsertAsync(
        Guid customerId,
        UpdateBeautyProfileRequest request,
        CancellationToken ct = default)
    {
        ValidateRequest(request);
        var profile = await repository.GetByCustomerIdAsync(customerId, ct);
        if (profile == null)
        {
            profile = CustomerBeautyProfile.Create(
                customerId,
                request.HairType,
                request.CurrentHairColor,
                request.Sensitivities,
                request.Preferences,
                request.Notes,
                request.SkinType,
                request.Allergies
            );
            await repository.AddAsync(profile, ct);
        }
        else
        {
            profile.Update(
                request.HairType,
                request.CurrentHairColor,
                request.Sensitivities,
                request.Preferences,
                request.Notes,
                request.SkinType,
                request.Allergies
            );
        }

        await unitOfWork.SaveChangesAsync(ct);
        return MapToDto(profile);
    }

    private static BeautyProfileDto MapToDto(CustomerBeautyProfile profile) =>
        new(
            profile.CustomerId,
            profile.HairType,
            profile.CurrentHairColor,
            profile.Sensitivities,
            profile.Preferences,
            profile.Notes,
            profile.SkinType,
            profile.Allergies,
            profile.UpdatedAt
        );

    private static void ValidateRequest(UpdateBeautyProfileRequest request)
    {
        if (request.HairType?.Length > 50)
            throw new ValidationException("HairType cannot exceed 50 characters.");

        if (request.CurrentHairColor?.Length > 100)
            throw new ValidationException("CurrentHairColor cannot exceed 100 characters.");

        if (request.Sensitivities?.Length > 1000)
            throw new ValidationException("Sensitivities cannot exceed 1000 characters.");

        if (request.Preferences?.Length > 1000)
            throw new ValidationException("Preferences cannot exceed 1000 characters.");

        if (request.Notes?.Length > 2000)
            throw new ValidationException("Notes cannot exceed 2000 characters.");

        if (request.SkinType?.Length > 50)
            throw new ValidationException("SkinType cannot exceed 50 characters.");

        if (request.Allergies?.Length > 1000)
            throw new ValidationException("Allergies cannot exceed 1000 characters.");
    }
}
