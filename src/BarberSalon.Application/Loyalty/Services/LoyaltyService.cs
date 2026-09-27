using BarberSalon.Application.Common.Interfaces;
using BarberSalon.Application.Customers.Interfaces;
using BarberSalon.Application.Loyalty.DTOs;
using BarberSalon.Application.Loyalty.Interfaces;
using BarberSalon.Domain.Loyalty.Entities;

namespace BarberSalon.Application.Loyalty.Services;

public sealed class LoyaltyService(
    ILoyaltyRepository loyaltyRepository,
    ICustomerRepository customerRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<List<LoyaltyAccountDto>> GetAllAccountsAsync(CancellationToken ct = default)
    {
        var accounts = await loyaltyRepository.GetAllAccountsAsync(ct);
        var customers = await customerRepository.GetAllAsync(ct);
        var customerMap = customers.ToDictionary(c => c.Id);

        return accounts.Select(a =>
        {
            customerMap.TryGetValue(a.CustomerId, out var customer);
            return new LoyaltyAccountDto(
                a.Id,
                a.CustomerId,
                a.PointsBalance,
                a.LifetimePoints,
                a.Tier,
                a.ReferralCode,
                a.UpdatedAt,
                customer?.FullName,
                customer?.PhoneNumber
            );
        }).ToList();
    }

    public async Task<LoyaltyAccountDto> RedeemPointsAsync(Guid customerId, int points, CancellationToken ct = default)
    {
        var account = await loyaltyRepository.GetAccountByCustomerIdAsync(customerId, ct);
        if (account is null)
        {
            account = LoyaltyAccount.Create(customerId);
            await loyaltyRepository.AddAccountAsync(account, ct);
        }

        if (points <= 0)
        {
            throw new InvalidOperationException("Points to redeem must be greater than zero.");
        }

        if (account.PointsBalance < points)
        {
            throw new InvalidOperationException("Insufficient points balance.");
        }

        account.RedeemPoints(points);
        var transaction = LoyaltyTransaction.Create(customerId, -points, "redeemed", "کسر امتیاز جهت دریافت پاداش");
        await loyaltyRepository.AddTransactionAsync(transaction, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var customer = await customerRepository.GetByIdAsync(customerId, ct);
        return new LoyaltyAccountDto(
            account.Id,
            account.CustomerId,
            account.PointsBalance,
            account.LifetimePoints,
            account.Tier,
            account.ReferralCode,
            account.UpdatedAt,
            customer?.FullName,
            customer?.PhoneNumber
        );
    }

    public async Task<LoyaltyAccountDto> GetOrCreateAccountAsync(Guid customerId, CancellationToken ct = default)
    {
        var account = await loyaltyRepository.GetAccountByCustomerIdAsync(customerId, ct);
        if (account is null)
        {
            account = LoyaltyAccount.Create(customerId);
            await loyaltyRepository.AddAccountAsync(account, ct);
            await unitOfWork.SaveChangesAsync(ct);
        }

        var customer = await customerRepository.GetByIdAsync(customerId, ct);
        return new LoyaltyAccountDto(
            account.Id,
            account.CustomerId,
            account.PointsBalance,
            account.LifetimePoints,
            account.Tier,
            account.ReferralCode,
            account.UpdatedAt,
            customer?.FullName,
            customer?.PhoneNumber
        );
    }

    public async Task<List<ReferralDto>> GetReferralsAsync(Guid? customerId = null, CancellationToken ct = default)
    {
        var referrals = await loyaltyRepository.GetReferralsAsync(customerId, ct);
        return referrals.Select(MapReferral).ToList();
    }

    public async Task<ValidateReferralResponse> ValidateReferralCodeAsync(string code, Guid? customerId = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return new ValidateReferralResponse(false, "Referral code cannot be empty.");
        }

        var normalized = code.Trim().ToUpperInvariant();
        var account = await loyaltyRepository.GetAccountByReferralCodeAsync(normalized, ct);
        if (account is null)
        {
            var referral = await loyaltyRepository.GetReferralByCodeAsync(normalized, ct);
            if (referral is null)
            {
                return new ValidateReferralResponse(false, "Invalid referral code.");
            }

            if (customerId.HasValue && referral.ReferrerCustomerId == customerId.Value)
            {
                return new ValidateReferralResponse(false, "You cannot use your own referral code.");
            }

            return new ValidateReferralResponse(true, "Referral code is valid.", referral.ReferrerName, referral.ReferredDiscountPercent);
        }

        if (customerId.HasValue && account.CustomerId == customerId.Value)
        {
            return new ValidateReferralResponse(false, "You cannot use your own referral code.");
        }

        var referrer = await customerRepository.GetByIdAsync(account.CustomerId, ct);
        var referrerName = referrer?.FullName ?? "Member";

        return new ValidateReferralResponse(true, "Referral code is valid.", referrerName, 15);
    }

    public async Task<ReferralDto> ApplyReferralAsync(ApplyReferralRequest req, CancellationToken ct = default)
    {
        var normalized = req.Code.Trim().ToUpperInvariant();
        var account = await loyaltyRepository.GetAccountByReferralCodeAsync(normalized, ct);

        Guid referrerId = Guid.Empty;
        string referrerName = "Member";
        int rewardPoints = 50;
        int discountPercent = 15;

        if (account != null)
        {
            referrerId = account.CustomerId;
            var referrer = await customerRepository.GetByIdAsync(account.CustomerId, ct);
            if (referrer != null) referrerName = referrer.FullName;
            account.AddPoints(rewardPoints);
        }

        string? referredName = null;
        string? referredPhone = null;
        if (req.CustomerId.HasValue)
        {
            var referredCustomer = await customerRepository.GetByIdAsync(req.CustomerId.Value, ct);
            if (referredCustomer != null)
            {
                referredName = referredCustomer.FullName;
                referredPhone = referredCustomer.PhoneNumber;
            }
        }

        var referral = Referral.Create(
            referrerId,
            referrerName,
            normalized,
            req.CustomerId,
            referredName,
            referredPhone,
            rewardPoints,
            discountPercent);

        if (req.CustomerId.HasValue)
        {
            referral.Complete(req.CustomerId.Value, referredName, referredPhone);
        }

        await loyaltyRepository.AddReferralAsync(referral, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return MapReferral(referral);
    }

    public async Task<List<LoyaltyTransactionDto>> GetTransactionsByCustomerIdAsync(Guid customerId, CancellationToken ct = default)
    {
        var txs = await loyaltyRepository.GetTransactionsByCustomerIdAsync(customerId, ct);
        return txs.Select(t => new LoyaltyTransactionDto(
            t.Id,
            t.CustomerId,
            t.Points,
            t.Type,
            t.Description,
            t.CreatedAt
        )).ToList();
    }

    public async Task<LoyaltyAccountDto> AdjustPointsAsync(Guid customerId, int points, string reason, CancellationToken ct = default)
    {
        var account = await loyaltyRepository.GetAccountByCustomerIdAsync(customerId, ct);
        if (account is null)
        {
            account = LoyaltyAccount.Create(customerId);
            await loyaltyRepository.AddAccountAsync(account, ct);
        }

        if (points >= 0)
        {
            account.AddPoints(points);
        }
        else
        {
            account.RedeemPoints(-points);
        }

        var tx = LoyaltyTransaction.Create(customerId, points, "adjusted", string.IsNullOrWhiteSpace(reason) ? "تنظیم دستی امتیاز توسط مدیر" : reason);
        await loyaltyRepository.AddTransactionAsync(tx, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var customer = await customerRepository.GetByIdAsync(customerId, ct);
        return new LoyaltyAccountDto(
            account.Id,
            account.CustomerId,
            account.PointsBalance,
            account.LifetimePoints,
            account.Tier,
            account.ReferralCode,
            account.UpdatedAt,
            customer?.FullName,
            customer?.PhoneNumber
        );
    }

    private static LoyaltyAccountDto MapAccount(LoyaltyAccount a) =>
        new(a.Id, a.CustomerId, a.PointsBalance, a.LifetimePoints, a.Tier, a.ReferralCode, a.UpdatedAt);

    private static ReferralDto MapReferral(Referral r) =>
        new(r.Id, r.ReferrerCustomerId, r.ReferrerName, r.ReferralCode, r.ReferredCustomerId, r.ReferredName, r.ReferredPhone, r.Status, r.ReferrerRewardPoints, r.ReferredDiscountPercent, r.CompletedAt, r.CreatedAt);
}
