using BarberSalon.Domain.Salons.Entities;

namespace BarberSalon.Domain.Salons.Repositories;

public interface ISalonRepository
{
    Task<Salon?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Salon?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<IReadOnlyList<Salon>> GetAllAsync(bool includeArchived = false, CancellationToken ct = default);
    Task AddAsync(Salon salon, CancellationToken ct = default);
    Task UpdateAsync(Salon salon, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken ct = default);
    Task<int> CountAsync(bool? isActive = null, CancellationToken ct = default);
}
