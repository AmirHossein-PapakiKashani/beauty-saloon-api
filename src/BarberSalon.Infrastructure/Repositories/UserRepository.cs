using BarberSalon.Application.Auth.Interfaces;
using BarberSalon.Domain.Auth.Entities;
using BarberSalon.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BarberSalon.Infrastructure.Repositories;

/// <inheritdoc cref="IUserRepository"/>
public sealed class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Initializes a new instance of <see cref="UserRepository"/>.
    /// </summary>
    /// <param name="context">The EF Core database context.</param>
    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<User?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
    }

    /// <inheritdoc/>
    public void Update(User user)
    {
        _context.Users.Update(user);
    }
}
