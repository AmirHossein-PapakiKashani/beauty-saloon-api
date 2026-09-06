using BarberSalon.Domain.Common;

namespace BarberSalon.Domain.Staff.Entities;

/// <summary>
/// An employee of the salon who performs services (e.g. Barber, Colorist, Stylist).
/// Acts as an independent Aggregate Root.
/// </summary>
public sealed class StaffMember : BaseEntity
{
    /// <summary>Full display name of the staff member.</summary>
    public string FullName { get; private set; } = default!;

    /// <summary>URL-friendly slug identifier for the staff member.</summary>
    public string Slug { get; private set; } = default!;

    /// <summary>Contact phone number.</summary>
    public string PhoneNumber { get; private set; } = default!;

    /// <summary>Bio / profile description shown to customers.</summary>
    public string Bio { get; private set; } = string.Empty;

    /// <summary>Primary professional role / title (e.g. "آرایشگر ارشد", "متخصص رنگ").</summary>
    public string Role { get; private set; } = default!;

    /// <summary>Whether the staff member is currently active and available for bookings.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Years of professional experience.</summary>
    public int YearsExperience { get; private set; }

    /// <summary>List of specialty tags (e.g. "fade", "balayage").</summary>
    public List<string> Specialties { get; private set; } = new();

    /// <summary>List of SalonService identifiers that this staff member can perform.</summary>
    public List<Guid> ServiceIds { get; private set; } = new();

    /// <summary>Optional linked User identifier for authentication.</summary>
    public Guid? UserId { get; private set; }

    /// <summary>Weekly working hours represented as JSON.</summary>
    public string WorkingHoursJson { get; private set; } = string.Empty;

    // Private parameterless constructor required by EF Core
    private StaffMember() { }

    /// <summary>
    /// Creates a new <see cref="StaffMember"/> instance with validation.
    /// </summary>
    /// <param name="fullName">Full display name. Required, max 100 characters.</param>
    /// <param name="slug">URL slug. Required, max 100 characters.</param>
    /// <param name="phoneNumber">Phone number. Required, max 20 characters.</param>
    /// <param name="bio">Optional bio. Max 1000 characters.</param>
    /// <param name="role">Professional role. Required, max 100 characters.</param>
    /// <param name="yearsExperience">Years of experience. Must be non-negative.</param>
    /// <param name="specialties">Optional list of specialties.</param>
    /// <param name="serviceIds">Optional list of performed service IDs.</param>
    /// <param name="userId">Optional linked user identity ID.</param>
    /// <param name="workingHoursJson">Optional working hours JSON string.</param>
    /// <returns>A new active <see cref="StaffMember"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public static StaffMember Create(
        string fullName,
        string slug,
        string phoneNumber,
        string bio,
        string role,
        int yearsExperience,
        List<string>? specialties = null,
        List<Guid>? serviceIds = null,
        Guid? userId = null,
        string? workingHoursJson = null)
    {
        Validate(fullName, slug, phoneNumber, bio, role, yearsExperience);

        return new StaffMember
        {
            FullName = fullName.Trim(),
            Slug = slug.Trim().ToLowerInvariant(),
            PhoneNumber = phoneNumber.Trim(),
            Bio = bio?.Trim() ?? string.Empty,
            Role = role.Trim(),
            IsActive = true,
            YearsExperience = yearsExperience,
            Specialties = specialties != null ? new List<string>(specialties) : new List<string>(),
            ServiceIds = serviceIds != null ? new List<Guid>(serviceIds) : new List<Guid>(),
            UserId = userId,
            WorkingHoursJson = workingHoursJson?.Trim() ?? string.Empty
        };
    }

    /// <summary>
    /// Updates staff member profile details.
    /// </summary>
    public void Update(
        string fullName,
        string slug,
        string phoneNumber,
        string bio,
        string role,
        int yearsExperience,
        List<string>? specialties = null,
        List<Guid>? serviceIds = null,
        Guid? userId = null,
        string? workingHoursJson = null)
    {
        Validate(fullName, slug, phoneNumber, bio, role, yearsExperience);

        FullName = fullName.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        PhoneNumber = phoneNumber.Trim();
        Bio = bio?.Trim() ?? string.Empty;
        Role = role.Trim();
        YearsExperience = yearsExperience;
        Specialties = specialties != null ? new List<string>(specialties) : new List<string>();
        ServiceIds = serviceIds != null ? new List<Guid>(serviceIds) : new List<Guid>();
        UserId = userId;
        WorkingHoursJson = workingHoursJson?.Trim() ?? string.Empty;
        Touch();
    }

    /// <summary>
    /// Archives the staff member (soft delete).
    /// </summary>
    /// <exception cref="DomainException">Thrown when already archived.</exception>
    public void Archive()
    {
        if (!IsActive)
            throw new DomainException("This staff member is already archived.");

        IsActive = false;
        Touch();
    }

    /// <summary>
    /// Reactivates an archived staff member.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        Touch();
    }

    /// <summary>
    /// Assigns a service to the staff member if not already assigned.
    /// </summary>
    /// <param name="serviceId">The service identifier.</param>
    public void AssignService(Guid serviceId)
    {
        if (!ServiceIds.Contains(serviceId))
        {
            ServiceIds.Add(serviceId);
            Touch();
        }
    }

    /// <summary>
    /// Removes an assigned service from the staff member.
    /// </summary>
    /// <param name="serviceId">The service identifier.</param>
    public void RemoveService(Guid serviceId)
    {
        if (ServiceIds.Remove(serviceId))
        {
            Touch();
        }
    }

    private static void Validate(
        string fullName,
        string slug,
        string phoneNumber,
        string? bio,
        string role,
        int yearsExperience)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new DomainException("Staff member full name cannot be empty.");

        if (fullName.Length > 100)
            throw new DomainException("Staff member full name cannot exceed 100 characters.");

        if (string.IsNullOrWhiteSpace(slug))
            throw new DomainException("Staff member slug cannot be empty.");

        if (slug.Length > 100)
            throw new DomainException("Staff member slug cannot exceed 100 characters.");

        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new DomainException("Staff member phone number cannot be empty.");

        if (phoneNumber.Length > 20)
            throw new DomainException("Staff member phone number cannot exceed 20 characters.");

        if (bio != null && bio.Length > 1000)
            throw new DomainException("Staff member bio cannot exceed 1000 characters.");

        if (string.IsNullOrWhiteSpace(role))
            throw new DomainException("Staff member role cannot be empty.");

        if (role.Length > 100)
            throw new DomainException("Staff member role cannot exceed 100 characters.");

        if (yearsExperience < 0)
            throw new DomainException("Years of experience cannot be negative.");
    }
}
