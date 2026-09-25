namespace BarberSalon.Application.Portfolio.DTOs;

public sealed record PortfolioItemDto(
    Guid Id,
    string Title,
    string Category,
    string BeforeImageUrl,
    string AfterImageUrl,
    string? Description,
    Guid? StaffId,
    DateTime CreatedAt,
    int LikesCount = 0,
    bool IsFeatured = false,
    bool IsPublished = true
);

public sealed record CreatePortfolioItemRequest(
    string Title,
    string Category,
    string BeforeImageUrl,
    string AfterImageUrl,
    string? Description = null,
    Guid? StaffId = null
);

public sealed record UpdatePortfolioItemRequest(
    string Title,
    string Category,
    string BeforeImageUrl,
    string AfterImageUrl,
    string? Description = null,
    Guid? StaffId = null
);

