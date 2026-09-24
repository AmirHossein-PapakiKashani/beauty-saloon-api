using BarberSalon.Domain.BeautyProfile.Entities;

namespace BarberSalon.Application.BeautyProfile.Interfaces;

public interface IBeautyProfileRepository
{
    Task<CustomerBeautyProfile?> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default);
    Task AddAsync(CustomerBeautyProfile profile, CancellationToken ct = default);
}
