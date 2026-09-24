using BarberSalon.Domain.Reviews.Entities;

namespace BarberSalon.Application.Reviews.Interfaces;

public interface IReviewRepository
{
    Task<List<Review>> GetAllAsync(
        Guid? customerId = null,
        Guid? staffId = null,
        Guid? serviceId = null,
        CancellationToken cancellationToken = default);

    Task<Review?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Review review, CancellationToken cancellationToken = default);

    void Remove(Review review);
}
