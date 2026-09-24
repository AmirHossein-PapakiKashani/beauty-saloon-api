using BarberSalon.Application.Common.Exceptions;
using BarberSalon.Application.Common.Interfaces;
using BarberSalon.Application.SalonServices.DTOs;
using BarberSalon.Application.SalonServices.Interfaces;
using BarberSalon.Domain.SalonServices.Entities;

namespace BarberSalon.Application.SalonServices.Services;

/// <summary>
/// Application-layer use cases for managing salon services.
/// Orchestrates domain logic, repository access, and persistence.
/// </summary>
public sealed class SalonServiceManager
{
    private readonly ISalonServiceRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Creates a new instance of <see cref="SalonServiceManager"/>.</summary>
    /// <param name="repository">Repository for salon services.</param>
    /// <param name="unitOfWork">Unit of work for committing changes.</param>
    public SalonServiceManager(ISalonServiceRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>Returns all active services as DTOs.</summary>
    public async Task<List<SalonServiceDto>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        var services = await _repository.GetAllActiveAsync(cancellationToken);
        return services.Select(MapToDto).ToList();
    }

    /// <summary>Returns all services (active and archived) as DTOs. For Admin use.</summary>
    public async Task<List<SalonServiceDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var services = await _repository.GetAllAsync(cancellationToken);
        return services.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Returns a single service by its identifier.
    /// </summary>
    /// <exception cref="NotFoundException">Thrown when no service with the given ID exists.</exception>
    public async Task<SalonServiceDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var service = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(SalonService), id);

        return MapToDto(service);
    }

    /// <summary>
    /// Creates and persists a new salon service.
    /// </summary>
    /// <exception cref="ValidationException">Thrown when a service with the same name already exists.</exception>
    public async Task<SalonServiceDto> CreateAsync(
        CreateSalonServiceRequest request,
        CancellationToken cancellationToken = default)
    {
        var nameExists = await _repository.ExistsByNameAsync(request.Name, cancellationToken);
        if (nameExists)
            throw new ValidationException($"A service named '{request.Name}' already exists.");

        var service = SalonService.Create(
            request.Name,
            request.Description,
            request.DurationMinutes,
            request.Price,
            request.Category);

        await _repository.AddAsync(service, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(service);
    }

    /// <summary>
    /// Updates an existing service's details.
    /// </summary>
    /// <exception cref="NotFoundException">Thrown when the service does not exist.</exception>
    public async Task<SalonServiceDto> UpdateAsync(
        Guid id,
        UpdateSalonServiceRequest request,
        CancellationToken cancellationToken = default)
    {
        var service = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(SalonService), id);

        service.Update(
            request.Name,
            request.Description,
            request.DurationMinutes,
            request.Price,
            request.Category);

        if (request.IsActive.HasValue)
        {
            service.SetActive(request.IsActive.Value);
        }

        _repository.Update(service);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(service);
    }

    /// <summary>
    /// Archives a service (soft delete). The service will no longer appear in public listings.
    /// </summary>
    /// <exception cref="NotFoundException">Thrown when the service does not exist.</exception>
    public async Task ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var service = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(SalonService), id);

        service.Archive();
        _repository.Update(service);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    // ─── Private Helpers ────────────────────────────────────────────────────

    private static SalonServiceDto MapToDto(SalonService s) => new(
        s.Id,
        s.Name,
        s.Description,
        s.DurationMinutes,
        s.Price,
        s.Category,
        s.IsActive,
        s.CreatedAt,
        s.UpdatedAt
    );
}
