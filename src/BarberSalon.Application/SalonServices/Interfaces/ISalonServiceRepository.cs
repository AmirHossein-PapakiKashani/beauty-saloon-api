using BarberSalon.Domain.SalonServices.Entities;

namespace BarberSalon.Application.SalonServices.Interfaces;

/// <summary>Data access contract for <see cref="SalonService"/> entities.</summary>
public interface ISalonServiceRepository
{
    /// <summary>Returns a service by its identifier, or null if not found.</summary>
    Task<SalonService?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns all active services ordered by Category then Name.</summary>
    Task<List<SalonService>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns all services (active and archived). For Admin use only.</summary>
    Task<List<SalonService>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns true if a service with the given name already exists.</summary>
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>Adds a new service to the EF Core change tracker.</summary>
    Task AddAsync(SalonService service, CancellationToken cancellationToken = default);

    /// <summary>Marks a modified service as updated in the EF Core change tracker.</summary>
    void Update(SalonService service);
}
