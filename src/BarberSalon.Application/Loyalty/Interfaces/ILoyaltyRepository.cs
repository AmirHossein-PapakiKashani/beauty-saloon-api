using BarberSalon.Domain.Loyalty.Entities;

namespace BarberSalon.Application.Loyalty.Interfaces;

public interface ILoyaltyRepository
{
    Task<List<LoyaltyAccount>> GetAllAccountsAsync(CancellationToken ct = default);
    Task<LoyaltyAccount?> GetAccountByCustomerIdAsync(Guid customerId, CancellationToken ct = default);
    Task<LoyaltyAccount?> GetAccountByReferralCodeAsync(string referralCode, CancellationToken ct = default);
    Task AddAccountAsync(LoyaltyAccount account, CancellationToken ct = default);
    Task<List<Referral>> GetReferralsAsync(Guid? customerId = null, CancellationToken ct = default);
    Task<Referral?> GetReferralByCodeAsync(string referralCode, CancellationToken ct = default);
    Task AddReferralAsync(Referral referral, CancellationToken ct = default);
}
