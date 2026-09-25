using BarberSalon.Domain.BeautyProfile.Entities;

namespace BarberSalon.Application.BeautyProfile.Interfaces;

public interface IBeautyProfileRepository
{
    Task<CustomerBeautyProfile?> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default);
    Task AddAsync(CustomerBeautyProfile profile, CancellationToken ct = default);
    Task<List<BeautyHistoryEntry>> GetHistoryByCustomerIdAsync(Guid customerId, CancellationToken ct = default);
    Task AddHistoryAsync(BeautyHistoryEntry entry, CancellationToken ct = default);
    Task<BeautyHistoryEntry?> GetHistoryByIdAsync(Guid id, CancellationToken ct = default);
    Task DeleteHistoryAsync(BeautyHistoryEntry entry, CancellationToken ct = default);
}
