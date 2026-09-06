using BarberSalon.Domain.Customers.Entities;

namespace BarberSalon.Application.Customers.Interfaces;

/// <summary>
/// Data access contract for <see cref="Customer"/> entities.
/// </summary>
public interface ICustomerRepository
{
    /// <summary>Returns a customer by their unique identifier, or null if not found.</summary>
    /// <param name="id">The customer identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The customer entity or null.</returns>
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns a customer by their normalized phone number, or null if not found.</summary>
    /// <param name="phoneNumber">The phone number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The customer entity or null.</returns>
    Task<Customer?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default);

    /// <summary>Returns true if a customer with the given phone number already exists.</summary>
    /// <param name="phoneNumber">The phone number to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if exists, otherwise false.</returns>
    Task<bool> ExistsByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default);

    /// <summary>Returns all active customers.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of active customer entities.</returns>
    Task<List<Customer>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns all customers (active and archived). For Admin use.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all customer entities.</returns>
    Task<List<Customer>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Searches active customers whose full name or phone number contains the specified query string.</summary>
    /// <param name="query">The search term to match against customer name or phone number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of matching active customer entities.</returns>
    Task<List<Customer>> SearchAsync(string query, CancellationToken cancellationToken = default);

    /// <summary>Adds a new customer to the EF Core change tracker.</summary>
    /// <param name="customer">The customer entity to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(Customer customer, CancellationToken cancellationToken = default);

    /// <summary>Marks a modified customer as updated in the EF Core change tracker.</summary>
    /// <param name="customer">The customer entity to update.</param>
    void Update(Customer customer);
}
