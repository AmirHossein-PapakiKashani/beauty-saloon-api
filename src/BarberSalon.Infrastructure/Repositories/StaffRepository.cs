using BarberSalon.Application.Staff.Interfaces;
using BarberSalon.Domain.Staff.Entities;
using BarberSalon.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BarberSalon.Infrastructure.Repositories;

/// <inheritdoc cref="IStaffRepository"/>
public sealed class StaffRepository : IStaffRepository
{
    private readonly AppDbContext _context;

    /// <summary>Initializes a new instance of <see cref="StaffRepository"/>.</summary>
    public StaffRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<StaffMember?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.StaffMembers
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<StaffMember?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        return await _context.StaffMembers
            .FirstOrDefaultAsync(s => s.Slug == normalizedSlug, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<StaffMember>> GetAllActiveAsync(CancellationToken cancellationToken = default)
        => await _context.StaffMembers
            .Where(s => s.IsActive)
            .OrderBy(s => s.FullName)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<List<StaffMember>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.StaffMembers
            .OrderBy(s => s.FullName)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<List<StaffMember>> GetByServiceIdAsync(Guid serviceId, CancellationToken cancellationToken = default)
    {
        var activeStaff = await _context.StaffMembers
            .Where(s => s.IsActive)
            .OrderBy(s => s.FullName)
            .ToListAsync(cancellationToken);

        return activeStaff.Where(s => s.ServiceIds.Contains(serviceId)).ToList();
    }

    /// <inheritdoc />
    public async Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        return await _context.StaffMembers
            .AnyAsync(s => s.Slug == normalizedSlug, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(StaffMember staffMember, CancellationToken cancellationToken = default)
        => await _context.StaffMembers.AddAsync(staffMember, cancellationToken);

    /// <inheritdoc />
    public void Update(StaffMember staffMember)
        => _context.StaffMembers.Update(staffMember);
}
