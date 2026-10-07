using BarberSalon.Domain.Salons.Entities;
using BarberSalon.Domain.Salons.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BarberSalon.Infrastructure.Persistence.Repositories;

public class SalonRepository : ISalonRepository
{
    private readonly AppDbContext _context;

    public SalonRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Salon?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Salons.FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<Salon?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        return await _context.Salons.FirstOrDefaultAsync(s => s.Slug == normalized, ct);
    }

    public async Task<IReadOnlyList<Salon>> GetAllAsync(bool includeArchived = false, CancellationToken ct = default)
    {
        var query = _context.Salons.AsNoTracking();
        if (!includeArchived)
        {
            query = query.Where(s => s.IsActive);
        }
        return await query.OrderByDescending(s => s.CreatedAt).ToListAsync(ct);
    }

    public async Task AddAsync(Salon salon, CancellationToken ct = default)
    {
        await _context.Salons.AddAsync(salon, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Salon salon, CancellationToken ct = default)
    {
        _context.Salons.Update(salon);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken ct = default)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        return await _context.Salons.AnyAsync(s => s.Slug == normalized && (!excludeId.HasValue || s.Id != excludeId.Value), ct);
    }

    public async Task<int> CountAsync(bool? isActive = null, CancellationToken ct = default)
    {
        var query = _context.Salons.AsQueryable();
        if (isActive.HasValue)
        {
            query = query.Where(s => s.IsActive == isActive.Value);
        }
        return await query.CountAsync(ct);
    }
}
