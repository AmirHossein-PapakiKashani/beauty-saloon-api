using BarberSalon.Application.Reviews.Interfaces;
using BarberSalon.Domain.Reviews.Entities;
using BarberSalon.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BarberSalon.Infrastructure.Repositories;

public sealed class ReviewRepository(AppDbContext context) : IReviewRepository
{
    public async Task<List<Review>> GetAllAsync(
        Guid? customerId = null,
        Guid? staffId = null,
        Guid? serviceId = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.Reviews.AsQueryable();

        if (customerId.HasValue && customerId.Value != Guid.Empty)
            query = query.Where(r => r.CustomerId == customerId.Value);

        if (staffId.HasValue && staffId.Value != Guid.Empty)
            query = query.Where(r => r.StaffId == staffId.Value);

        if (serviceId.HasValue && serviceId.Value != Guid.Empty)
            query = query.Where(r => r.ServiceId == serviceId.Value);

        return await query.OrderByDescending(r => r.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<Review?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Reviews.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task AddAsync(Review review, CancellationToken cancellationToken = default)
    {
        await context.Reviews.AddAsync(review, cancellationToken);
    }

    public void Remove(Review review)
    {
        context.Reviews.Remove(review);
    }
}
