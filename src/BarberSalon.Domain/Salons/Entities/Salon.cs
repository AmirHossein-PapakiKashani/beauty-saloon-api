using BarberSalon.Domain.Common;

namespace BarberSalon.Domain.Salons.Entities;

public sealed class Salon : BaseEntity
{
    public string Name { get; private set; } = default!;
    public string Slug { get; private set; } = default!;
    public string PhoneNumber { get; private set; } = default!;
    public string Address { get; private set; } = default!;
    public string? Description { get; private set; }
    public Guid? OwnerUserId { get; private set; }
    public bool IsActive { get; private set; }

    private Salon() : base() { }

    private Salon(
        string name,
        string slug,
        string phoneNumber,
        string address,
        DateTime createdAt,
        Guid? ownerUserId,
        string? description)
        : base(createdAt)
    {
        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        PhoneNumber = phoneNumber.Trim();
        Address = address.Trim();
        OwnerUserId = ownerUserId;
        Description = description?.Trim();
        IsActive = true;
    }

    public static Salon Create(
        string name,
        string slug,
        string phoneNumber,
        string address,
        DateTime createdAt,
        Guid? ownerUserId = null,
        string? description = null)
    {
        Validate(name, slug, phoneNumber, address);
        return new Salon(name, slug, phoneNumber, address, createdAt, ownerUserId, description);
    }

    public void UpdateDetails(string name, string phoneNumber, string address, string? description)
    {
        Validate(name, Slug, phoneNumber, address);
        Name = name.Trim();
        PhoneNumber = phoneNumber.Trim();
        Address = address.Trim();
        Description = description?.Trim();
        Touch();
    }

    public void AssignOwner(Guid ownerUserId)
    {
        OwnerUserId = ownerUserId;
        Touch();
    }

    public void Archive()
    {
        if (!IsActive)
        {
            throw new DomainException("This salon is already archived.");
        }

        IsActive = false;
        Touch();
    }

    public void Activate()
    {
        IsActive = true;
        Touch();
    }

    private static void Validate(string name, string slug, string phoneNumber, string address)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Salon name is required.");
        if (name.Length > 100)
            throw new DomainException("Salon name cannot exceed 100 characters.");
        if (string.IsNullOrWhiteSpace(slug))
            throw new DomainException("Salon slug is required.");
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new DomainException("Salon phone number is required.");
        if (string.IsNullOrWhiteSpace(address))
            throw new DomainException("Salon address is required.");
    }
}
