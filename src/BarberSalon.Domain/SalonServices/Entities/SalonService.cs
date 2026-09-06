using BarberSalon.Domain.Common;

namespace BarberSalon.Domain.SalonServices.Entities;

/// <summary>
/// A service offered by the salon — e.g. Haircut, Beard Trim, Hair Colour.
/// Acts as an independent Aggregate Root.
/// </summary>
public sealed class SalonService : BaseEntity
{
    /// <summary>Display name of the service. Must be unique across the salon.</summary>
    public string Name { get; private set; } = default!;

    /// <summary>Short description shown to customers during booking.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Estimated duration of the service in minutes.</summary>
    public int DurationMinutes { get; private set; }

    /// <summary>Price of the service in the local currency (Toman).</summary>
    public decimal Price { get; private set; }

    /// <summary>Category grouping — e.g. "Hair", "Beard", "Skin".</summary>
    public string Category { get; private set; } = default!;

    /// <summary>Whether the service is active and bookable. False means archived.</summary>
    public bool IsActive { get; private set; }

    // Private parameterless constructor required by EF Core
    private SalonService() { }

    /// <summary>
    /// Creates a new <see cref="SalonService"/> with validation.
    /// This is the only public way to instantiate this entity.
    /// </summary>
    /// <param name="name">Service name. Must not be empty. Max 100 chars.</param>
    /// <param name="description">Optional description. Max 500 chars.</param>
    /// <param name="durationMinutes">Duration in minutes. Must be between 1 and 480.</param>
    /// <param name="price">Price in Toman. Must not be negative.</param>
    /// <param name="category">Category name. Must not be empty. Max 50 chars.</param>
    /// <returns>A new active <see cref="SalonService"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when any validation rule fails.</exception>
    public static SalonService Create(
        string name,
        string description,
        int durationMinutes,
        decimal price,
        string category)
    {
        Validate(name, durationMinutes, price, category);

        return new SalonService
        {
            Name = name.Trim(),
            Description = description?.Trim() ?? string.Empty,
            DurationMinutes = durationMinutes,
            Price = price,
            Category = category.Trim(),
            IsActive = true
        };
    }

    /// <summary>
    /// Creates a new <see cref="SalonService"/> with validation without an initial description.
    /// </summary>
    /// <param name="name">Service name. Must not be empty. Max 100 chars.</param>
    /// <param name="durationMinutes">Duration in minutes. Must be between 1 and 480.</param>
    /// <param name="price">Price in Toman. Must not be negative.</param>
    /// <param name="category">Category name. Must not be empty. Max 50 chars.</param>
    /// <returns>A new active <see cref="SalonService"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when any validation rule fails.</exception>
    public static SalonService Create(
        string name,
        int durationMinutes,
        decimal price,
        string category)
        => Create(name, string.Empty, durationMinutes, price, category);

    /// <summary>
    /// Updates the service details. Applies the same validation rules as <see cref="Create(string, string, int, decimal, string)"/>.
    /// </summary>
    /// <exception cref="DomainException">Thrown when any validation rule fails.</exception>
    public void Update(
        string name,
        string description,
        int durationMinutes,
        decimal price,
        string category)
    {
        Validate(name, durationMinutes, price, category);

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        DurationMinutes = durationMinutes;
        Price = price;
        Category = category.Trim();
        Touch();
    }

    /// <summary>
    /// Archives the service. Archived services do not appear in public listings
    /// and cannot be selected during booking.
    /// </summary>
    /// <exception cref="DomainException">Thrown when the service is already archived.</exception>
    public void Archive()
    {
        if (!IsActive)
            throw new DomainException("This service is already archived.");

        IsActive = false;
        Touch();
    }

    /// <summary>Reactivates a previously archived service.</summary>
    public void Activate()
    {
        IsActive = true;
        Touch();
    }

    // ─── Private Validation ─────────────────────────────────────────────────

    private static void Validate(string name, int durationMinutes, decimal price, string category)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Service name cannot be empty.");

        if (name.Length > 100)
            throw new DomainException("Service name cannot exceed 100 characters.");

        if (durationMinutes <= 0)
            throw new DomainException("Duration must be a positive number of minutes.");

        if (durationMinutes > 480)
            throw new DomainException("Duration cannot exceed 480 minutes (8 hours).");

        if (price < 0)
            throw new DomainException("Price cannot be negative.");

        if (string.IsNullOrWhiteSpace(category))
            throw new DomainException("Category cannot be empty.");

        if (category.Length > 50)
            throw new DomainException("Category cannot exceed 50 characters.");
    }
}
