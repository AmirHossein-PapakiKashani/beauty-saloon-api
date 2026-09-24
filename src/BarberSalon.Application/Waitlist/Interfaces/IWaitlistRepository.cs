using BarberSalon.Domain.Waitlist.Entities;

namespace BarberSalon.Application.Waitlist.Interfaces;

public interface IWaitlistRepository
{
    Task<List<WaitlistEntry>> GetAllAsync(
        string? status = null,
        Guid? staffId = null,
        string? date = null,
        CancellationToken cancellationToken = default);

    Task<WaitlistEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<int> GetCountForSlotAsync(Guid staffId, string date, string time, CancellationToken cancellationToken = default);

    Task AddAsync(WaitlistEntry entry, CancellationToken cancellationToken = default);

    void Remove(WaitlistEntry entry);
}
