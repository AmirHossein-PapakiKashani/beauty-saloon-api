using BarberSalon.Application.BeautyProfile.Interfaces;
using BarberSalon.Domain.BeautyProfile.Entities;
using BarberSalon.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BarberSalon.Infrastructure.Repositories;

public sealed class BeautyProfileRepository(AppDbContext context) : IBeautyProfileRepository
{
    public async Task<CustomerBeautyProfile?> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default)
    {
        return await context.BeautyProfiles.FirstOrDefaultAsync(b => b.CustomerId == customerId, ct);
    }

    public async Task AddAsync(CustomerBeautyProfile profile, CancellationToken ct = default)
    {
        await context.BeautyProfiles.AddAsync(profile, ct);
    }

    public async Task<List<BeautyHistoryEntry>> GetHistoryByCustomerIdAsync(Guid customerId, CancellationToken ct = default)
    {
        return await context.BeautyHistoryEntries
            .Where(b => b.CustomerId == customerId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task AddHistoryAsync(BeautyHistoryEntry entry, CancellationToken ct = default)
    {
        await context.BeautyHistoryEntries.AddAsync(entry, ct);
    }

    public async Task<BeautyHistoryEntry?> GetHistoryByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await context.BeautyHistoryEntries.FirstOrDefaultAsync(b => b.Id == id, ct);
    }

    public Task DeleteHistoryAsync(BeautyHistoryEntry entry, CancellationToken ct = default)
    {
        context.BeautyHistoryEntries.Remove(entry);
        return Task.CompletedTask;
    }
}
