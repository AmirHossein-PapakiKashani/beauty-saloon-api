using BarberSalon.Application.Portfolio.Interfaces;
using BarberSalon.Domain.Portfolio.Entities;
using BarberSalon.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BarberSalon.Infrastructure.Repositories;

public sealed class PortfolioRepository(AppDbContext context) : IPortfolioRepository
{
    public async Task<List<PortfolioItem>> GetAllAsync(string? category = null, Guid? staffId = null, CancellationToken ct = default)
    {
        var query = context.PortfolioItems.AsQueryable();
        if (!string.IsNullOrWhiteSpace(category) && !category.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(p => p.Category == category);
        }
        if (staffId.HasValue && staffId.Value != Guid.Empty)
        {
            query = query.Where(p => p.StaffId == staffId.Value);
        }
        return await query.OrderByDescending(p => p.CreatedAt).ToListAsync(ct);
    }

    public async Task<PortfolioItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await context.PortfolioItems.FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task AddAsync(PortfolioItem item, CancellationToken ct = default)
    {
        await context.PortfolioItems.AddAsync(item, ct);
    }

    public void Remove(PortfolioItem item)
    {
        context.PortfolioItems.Remove(item);
    }
}
