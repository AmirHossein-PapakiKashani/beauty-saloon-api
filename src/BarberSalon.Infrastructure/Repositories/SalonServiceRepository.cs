using BarberSalon.Application.SalonServices.Interfaces;
using BarberSalon.Domain.SalonServices.Entities;
using BarberSalon.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BarberSalon.Infrastructure.Repositories;

/// <inheritdoc cref="ISalonServiceRepository"/>
public sealed class SalonServiceRepository : ISalonServiceRepository
{
    private readonly AppDbContext _context;

    public SalonServiceRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<SalonService?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.SalonServices
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<List<SalonService>> GetAllActiveAsync(CancellationToken cancellationToken = default)
        => await _context.SalonServices
            .Where(s => s.IsActive)
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Name)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<List<SalonService>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.SalonServices
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Name)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
        => await _context.SalonServices
            .AnyAsync(s => s.Name == name.Trim(), cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(SalonService service, CancellationToken cancellationToken = default)
        => await _context.SalonServices.AddAsync(service, cancellationToken);

    /// <inheritdoc />
    public void Update(SalonService service)
        => _context.SalonServices.Update(service);
}
