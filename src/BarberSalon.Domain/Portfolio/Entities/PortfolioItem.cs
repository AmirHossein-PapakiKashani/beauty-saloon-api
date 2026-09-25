using BarberSalon.Domain.Common;

namespace BarberSalon.Domain.Portfolio.Entities;

public sealed class PortfolioItem : BaseEntity
{
    private PortfolioItem() { }

    public string Title { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public string BeforeImageUrl { get; private set; } = string.Empty;
    public string AfterImageUrl { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid? StaffId { get; private set; }
    public int LikesCount { get; private set; }
    public bool IsFeatured { get; private set; }
    public bool IsPublished { get; private set; } = true;

    public void IncrementLikes()
    {
        LikesCount++;
        Touch();
    }

    public void ToggleFeatured()
    {
        IsFeatured = !IsFeatured;
        Touch();
    }

    public void TogglePublished()
    {
        IsPublished = !IsPublished;
        Touch();
    }

    public static PortfolioItem Create(
        string title,
        string category,
        string beforeImageUrl,
        string afterImageUrl,
        string? description = null,
        Guid? staffId = null)
    {
        return new PortfolioItem
        {
            Title = title.Trim(),
            Category = category.Trim(),
            BeforeImageUrl = beforeImageUrl.Trim(),
            AfterImageUrl = afterImageUrl.Trim(),
            Description = description?.Trim(),
            StaffId = staffId
        };
    }

    public void Update(
        string title,
        string category,
        string beforeImageUrl,
        string afterImageUrl,
        string? description = null,
        Guid? staffId = null)
    {
        Title = title.Trim();
        Category = category.Trim();
        BeforeImageUrl = beforeImageUrl.Trim();
        AfterImageUrl = afterImageUrl.Trim();
        Description = description?.Trim();
        StaffId = staffId;
    }
}

