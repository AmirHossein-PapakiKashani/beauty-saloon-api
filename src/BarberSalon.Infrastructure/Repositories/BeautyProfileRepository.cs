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
}
