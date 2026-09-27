namespace BarberSalon.Application.Loyalty.DTOs;

public sealed record LoyaltyTransactionDto(
    Guid Id,
    Guid CustomerId,
    int Points,
    string Type,
    string Description,
    DateTime CreatedAt
);
