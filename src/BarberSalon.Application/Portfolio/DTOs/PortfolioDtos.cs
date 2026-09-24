namespace BarberSalon.Application.Portfolio.DTOs;

public sealed record PortfolioItemDto(
    Guid Id,
    string Title,
    string Category,
    string BeforeImageUrl,
    string AfterImageUrl,
    string? Description,
    Guid? StaffId,
    DateTime CreatedAt
);

public sealed record CreatePortfolioItemRequest(
    string Title,
    string Category,
    string BeforeImageUrl,
    string AfterImageUrl,
    string? Description = null,
    Guid? StaffId = null
);
