namespace BarberSalon.Domain.Common;

/// <summary>
/// Base class for all domain entities.
/// Manages identity, creation timestamp, and last-updated timestamp.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>Unique identifier. Set once in the constructor.</summary>
    public Guid Id { get; private set; }

    /// <summary>UTC timestamp of when this entity was created.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>UTC timestamp of the last modification.</summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>Initializes a new entity instance with generated GUID and current UTC timestamp.</summary>
    protected BaseEntity()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Initializes a new entity instance with a specified creation timestamp.</summary>
    /// <param name="createdAt">Explicit UTC creation timestamp.</param>
    protected BaseEntity(DateTime createdAt)
    {
        Id = Guid.NewGuid();
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    /// <summary>
    /// Updates <see cref="UpdatedAt"/> to the current UTC time.
    /// Call this at the end of every mutation method inside an Entity.
    /// </summary>
    protected void Touch() => UpdatedAt = DateTime.UtcNow;
}
