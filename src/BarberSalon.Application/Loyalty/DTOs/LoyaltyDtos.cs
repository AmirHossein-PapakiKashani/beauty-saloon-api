namespace BarberSalon.Application.Loyalty.DTOs;

public sealed record LoyaltyAccountDto(
    Guid Id,
    Guid CustomerId,
    int PointsBalance,
    int LifetimePoints,
    string Tier,
    string ReferralCode,
    DateTime UpdatedAt
);

public sealed record ReferralDto(
    Guid Id,
    Guid ReferrerCustomerId,
    string ReferrerName,
    string ReferralCode,
    Guid? ReferredCustomerId,
    string? ReferredName,
    string? ReferredPhone,
    string Status,
    int ReferrerRewardPoints,
    int ReferredDiscountPercent,
    DateTime? CompletedAt,
    DateTime CreatedAt
);

public sealed record ValidateReferralResponse(
    bool Valid,
    string Message,
    string? ReferrerName = null,
    int? DiscountPercent = null
);

public sealed record ApplyReferralRequest(
    string Code,
    Guid? CustomerId = null
);
