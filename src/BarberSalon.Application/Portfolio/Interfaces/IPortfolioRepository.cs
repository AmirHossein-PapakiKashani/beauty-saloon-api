using BarberSalon.Domain.Portfolio.Entities;

namespace BarberSalon.Application.Portfolio.Interfaces;

public interface IPortfolioRepository
{
    Task<List<PortfolioItem>> GetAllAsync(string? category = null, Guid? staffId = null, CancellationToken ct = default);
    Task<PortfolioItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(PortfolioItem item, CancellationToken ct = default);
    void Remove(PortfolioItem item);
}
