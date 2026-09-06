using BarberSalon.Domain.Auth.Entities;

namespace BarberSalon.Application.Auth.Interfaces;

/// <summary>
/// Data access contract for <see cref="User"/> entities.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Finds a user by their phone number.
    /// </summary>
    /// <param name="phoneNumber">The normalized phone number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The <see cref="User"/> entity or null if not found.</returns>
    Task<User?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a user by their unique identifier.
    /// </summary>
    /// <param name="id">The user GUID identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The <see cref="User"/> entity or null if not found.</returns>
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new user to the repository.
    /// </summary>
    /// <param name="user">The user entity to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a modified user as updated in the EF Core change tracker.
    /// </summary>
    /// <param name="user">The user entity to update.</param>
    void Update(User user);
}
