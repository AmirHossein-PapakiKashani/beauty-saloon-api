using BarberSalon.Application.Loyalty.Interfaces;
using BarberSalon.Domain.Loyalty.Entities;
using BarberSalon.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BarberSalon.Infrastructure.Repositories;

public sealed class LoyaltyRepository(AppDbContext context) : ILoyaltyRepository
{
    public async Task<List<LoyaltyAccount>> GetAllAccountsAsync(CancellationToken ct = default)
    {
        return await context.LoyaltyAccounts.OrderByDescending(l => l.PointsBalance).ToListAsync(ct);
    }

    public async Task<LoyaltyAccount?> GetAccountByCustomerIdAsync(Guid customerId, CancellationToken ct = default)
    {
        return await context.LoyaltyAccounts.FirstOrDefaultAsync(l => l.CustomerId == customerId, ct);
    }

    public async Task<LoyaltyAccount?> GetAccountByReferralCodeAsync(string referralCode, CancellationToken ct = default)
    {
        var normalized = referralCode.Trim().ToUpperInvariant();
        return await context.LoyaltyAccounts.FirstOrDefaultAsync(l => l.ReferralCode == normalized, ct);
    }

    public async Task AddAccountAsync(LoyaltyAccount account, CancellationToken ct = default)
    {
        await context.LoyaltyAccounts.AddAsync(account, ct);
    }

    public async Task<List<Referral>> GetReferralsAsync(Guid? customerId = null, CancellationToken ct = default)
    {
        var query = context.Referrals.AsQueryable();
        if (customerId.HasValue && customerId.Value != Guid.Empty)
        {
            query = query.Where(r => r.ReferrerCustomerId == customerId.Value || r.ReferredCustomerId == customerId.Value);
        }
        return await query.OrderByDescending(r => r.CreatedAt).ToListAsync(ct);
    }

    public async Task<Referral?> GetReferralByCodeAsync(string referralCode, CancellationToken ct = default)
    {
        var normalized = referralCode.Trim().ToUpperInvariant();
        return await context.Referrals.FirstOrDefaultAsync(r => r.ReferralCode == normalized, ct);
    }

    public async Task AddReferralAsync(Referral referral, CancellationToken ct = default)
    {
        await context.Referrals.AddAsync(referral, ct);
    }

    public async Task<List<LoyaltyTransaction>> GetTransactionsByCustomerIdAsync(Guid customerId, CancellationToken ct = default)
    {
        return await context.LoyaltyTransactions
            .Where(t => t.CustomerId == customerId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task AddTransactionAsync(LoyaltyTransaction transaction, CancellationToken ct = default)
    {
        await context.LoyaltyTransactions.AddAsync(transaction, ct);
    }
}
