using BarberSalon.Domain.Common;

namespace BarberSalon.Domain.Loyalty.Entities;

public sealed class LoyaltyAccount : BaseEntity
{
    private LoyaltyAccount() { }

    public Guid CustomerId { get; private set; }
    public int PointsBalance { get; private set; }
    public int LifetimePoints { get; private set; }
    public string Tier { get; private set; } = "bronze"; // bronze | silver | gold | platinum
    public string ReferralCode { get; private set; } = string.Empty;

    public static LoyaltyAccount Create(Guid customerId, string? customCode = null)
    {
        var code = string.IsNullOrWhiteSpace(customCode)
            ? Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()
            : customCode.Trim().ToUpperInvariant();

        return new LoyaltyAccount
        {
            CustomerId = customerId,
            PointsBalance = 0,
            LifetimePoints = 0,
            Tier = "bronze",
            ReferralCode = code
        };
    }

    public void AddPoints(int amount)
    {
        if (amount <= 0) return;
        PointsBalance += amount;
        LifetimePoints += amount;
        UpdateTier();
        Touch();
    }

    public bool RedeemPoints(int amount)
    {
        if (amount <= 0 || PointsBalance < amount) return false;
        PointsBalance -= amount;
        Touch();
        return true;
    }

    private void UpdateTier()
    {
        Tier = LifetimePoints switch
        {
            >= 5000 => "platinum",
            >= 2000 => "gold",
            >= 500 => "silver",
            _ => "bronze"
        };
    }
}
