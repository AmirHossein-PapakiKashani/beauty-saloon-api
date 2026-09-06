using BarberSalon.Application.Customers.Interfaces;
using BarberSalon.Domain.Customers.Entities;
using BarberSalon.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BarberSalon.Infrastructure.Repositories;

/// <inheritdoc cref="ICustomerRepository"/>
public sealed class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _context;

    public CustomerRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<Customer?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
        => await _context.Customers.FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber.Trim(), cancellationToken);

    /// <inheritdoc />
    public async Task<bool> ExistsByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
        => await _context.Customers.AnyAsync(c => c.PhoneNumber == phoneNumber.Trim(), cancellationToken);

    /// <inheritdoc />
    public async Task<List<Customer>> GetAllActiveAsync(CancellationToken cancellationToken = default)
        => await _context.Customers.Where(c => c.IsActive).OrderBy(c => c.FullName).ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<List<Customer>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.Customers.OrderBy(c => c.FullName).ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<List<Customer>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return await GetAllActiveAsync(cancellationToken);
        }

        var q = query.Trim().ToLower();
        return await _context.Customers
            .Where(c => c.IsActive && (c.FullName.ToLower().Contains(q) || c.PhoneNumber.Contains(q)))
            .OrderBy(c => c.FullName)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
        => await _context.Customers.AddAsync(customer, cancellationToken);

    /// <inheritdoc />
    public void Update(Customer customer)
        => _context.Customers.Update(customer);
}
