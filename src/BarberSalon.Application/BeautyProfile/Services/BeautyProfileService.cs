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

    public async Task<List<BeautyHistoryEntryDto>> GetHistoryAsync(Guid customerId, CancellationToken ct = default)
    {
        var entries = await repository.GetHistoryByCustomerIdAsync(customerId, ct);
        return entries.Select(MapHistoryToDto).ToList();
    }

    public async Task<BeautyHistoryEntryDto> AddHistoryEntryAsync(
        Guid customerId,
        CreateBeautyHistoryEntryRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.ServiceName))
            throw new ValidationException("ServiceName is required.");

        var entry = BeautyHistoryEntry.Create(
            customerId,
            request.ServiceName,
            request.StaffName,
            request.Date,
            request.Formula,
            request.Notes,
            request.PhotoUrl
        );

        await repository.AddHistoryAsync(entry, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return MapHistoryToDto(entry);
    }

    public async Task<bool> DeleteHistoryEntryAsync(Guid id, CancellationToken ct = default)
    {
        var entry = await repository.GetHistoryByIdAsync(id, ct);
        if (entry == null) return false;

        await repository.DeleteHistoryAsync(entry, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return true;
    }

    private static BeautyHistoryEntryDto MapHistoryToDto(BeautyHistoryEntry entry) =>
        new(
            entry.Id,
            entry.CustomerId,
            entry.ServiceName,
            entry.StaffName,
            entry.Date,
            entry.Formula,
            entry.Notes,
            entry.PhotoUrl,
            entry.CreatedAt
        );
}
