using BarberSalon.Domain.Common;

namespace BarberSalon.Domain.Loyalty.Entities;

public sealed class LoyaltyTransaction : BaseEntity
{
    private LoyaltyTransaction() { }

    public Guid CustomerId { get; private set; }
    public int Points { get; private set; }
    public string Type { get; private set; } = string.Empty; // "earned", "redeemed", "adjusted"
    public string Description { get; private set; } = string.Empty;

    public static LoyaltyTransaction Create(Guid customerId, int points, string type, string description)
    {
        return new LoyaltyTransaction
        {
            CustomerId = customerId,
            Points = points,
            Type = type,
            Description = description
        };
    }
}
