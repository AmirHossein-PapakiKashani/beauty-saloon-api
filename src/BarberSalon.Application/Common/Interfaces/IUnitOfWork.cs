namespace BarberSalon.Application.Common.Interfaces;

/// <summary>
/// Unit of Work abstraction. Persists all pending EF Core changes in a single transaction.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Commits all tracked changes to the database.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
