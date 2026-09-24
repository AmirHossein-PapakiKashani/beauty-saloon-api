using BarberSalon.Domain.Common;

namespace BarberSalon.Domain.Loyalty.Entities;

public sealed class Referral : BaseEntity
{
    private Referral() { }

    public Guid ReferrerCustomerId { get; private set; }
    public string ReferrerName { get; private set; } = string.Empty;
    public string ReferralCode { get; private set; } = string.Empty;
    public Guid? ReferredCustomerId { get; private set; }
    public string? ReferredName { get; private set; }
    public string? ReferredPhone { get; private set; }
    public string Status { get; private set; } = "pending"; // pending | completed | expired
    public int ReferrerRewardPoints { get; private set; } = 50;
    public int ReferredDiscountPercent { get; private set; } = 15;
    public DateTime? CompletedAt { get; private set; }

    public static Referral Create(
        Guid referrerCustomerId,
        string referrerName,
        string referralCode,
        Guid? referredCustomerId = null,
        string? referredName = null,
        string? referredPhone = null,
        int referrerRewardPoints = 50,
        int referredDiscountPercent = 15)
    {
        return new Referral
        {
            ReferrerCustomerId = referrerCustomerId,
            ReferrerName = (referrerName ?? string.Empty).Trim(),
            ReferralCode = referralCode.Trim().ToUpperInvariant(),
            ReferredCustomerId = referredCustomerId,
            ReferredName = referredName?.Trim(),
            ReferredPhone = referredPhone?.Trim(),
            Status = "pending",
            ReferrerRewardPoints = referrerRewardPoints,
            ReferredDiscountPercent = referredDiscountPercent
        };
    }

    public void Complete(Guid referredCustomerId, string? referredName = null, string? referredPhone = null)
    {
        ReferredCustomerId = referredCustomerId;
        if (!string.IsNullOrWhiteSpace(referredName)) ReferredName = referredName.Trim();
        if (!string.IsNullOrWhiteSpace(referredPhone)) ReferredPhone = referredPhone.Trim();
        Status = "completed";
        CompletedAt = DateTime.UtcNow;
        Touch();
    }
}
