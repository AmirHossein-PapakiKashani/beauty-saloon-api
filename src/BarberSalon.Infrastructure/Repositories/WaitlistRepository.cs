using BarberSalon.Application.Waitlist.Interfaces;
using BarberSalon.Domain.Waitlist.Entities;
using BarberSalon.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BarberSalon.Infrastructure.Repositories;

public sealed class WaitlistRepository(AppDbContext context) : IWaitlistRepository
{
    public async Task<List<WaitlistEntry>> GetAllAsync(
        string? status = null,
        Guid? staffId = null,
        string? date = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.WaitlistEntries.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(w => w.Status == status);

        if (staffId.HasValue && staffId.Value != Guid.Empty)
            query = query.Where(w => w.StaffId == staffId.Value);

        if (!string.IsNullOrWhiteSpace(date))
            query = query.Where(w => w.Date == date);

        return await query.OrderBy(w => w.QueuePosition).ThenBy(w => w.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<WaitlistEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.WaitlistEntries.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public async Task<int> GetCountForSlotAsync(Guid staffId, string date, string time, CancellationToken cancellationToken = default)
    {
        return await context.WaitlistEntries
            .CountAsync(w => w.StaffId == staffId && w.Date == date && w.Time == time && w.Status == "waiting", cancellationToken);
    }

    public async Task AddAsync(WaitlistEntry entry, CancellationToken cancellationToken = default)
    {
        await context.WaitlistEntries.AddAsync(entry, cancellationToken);
    }

    public void Remove(WaitlistEntry entry)
    {
        context.WaitlistEntries.Remove(entry);
    }
}
