using BarberSalon.Application.Auth.Interfaces;
using BarberSalon.Domain.Auth.Entities;
using BarberSalon.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BarberSalon.Infrastructure.Repositories;

/// <inheritdoc cref="IOtpCodeRepository"/>
public sealed class OtpCodeRepository : IOtpCodeRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Initializes a new instance of <see cref="OtpCodeRepository"/>.
    /// </summary>
    /// <param name="context">The EF Core database context.</param>
    public OtpCodeRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<OtpCode?> GetActiveByPhoneNumberAsync(string phoneNumber, DateTime currentUtc, CancellationToken cancellationToken = default)
    {
        return await _context.OtpCodes
            .Where(o => o.PhoneNumber == phoneNumber && !o.IsUsed && o.ExpiresAt > currentUtc)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<OtpCode>> GetActiveListByPhoneNumberAsync(string phoneNumber, DateTime currentUtc, CancellationToken cancellationToken = default)
    {
        return await _context.OtpCodes
            .Where(o => o.PhoneNumber == phoneNumber && !o.IsUsed && o.ExpiresAt > currentUtc)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task AddAsync(OtpCode otpCode, CancellationToken cancellationToken = default)
    {
        await _context.OtpCodes.AddAsync(otpCode, cancellationToken);
    }
}
