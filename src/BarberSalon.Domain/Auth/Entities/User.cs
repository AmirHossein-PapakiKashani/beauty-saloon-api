using BarberSalon.Domain.Auth.Enums;
using BarberSalon.Domain.Common;

namespace BarberSalon.Domain.Auth.Entities;

/// <summary>
/// Represents the authentication identity of a person in the salon system.
/// Acts as an independent Aggregate Root in the Auth domain.
/// </summary>
public sealed class User : BaseEntity
{
    /// <summary>Normalized contact phone number used for login and notifications.</summary>
    public string PhoneNumber { get; private set; } = default!;

    /// <summary>Optional display full name.</summary>
    public string FullName { get; private set; } = string.Empty;

    /// <summary>Access role assigned to the user.</summary>
    public UserRole Role { get; private set; }

    /// <summary>Whether the user account is active.</summary>
    public bool IsActive { get; private set; }

    // Parameterless constructor for EF Core
    private User() : base() { }

    private User(string phoneNumber, DateTime createdAt, UserRole role = UserRole.Customer, string? fullName = null)
        : base(createdAt)
    {
        PhoneNumber = phoneNumber;
        Role = role;
        FullName = fullName?.Trim() ?? string.Empty;
        IsActive = true;
    }

    /// <summary>
    /// Creates a new <see cref="User"/> instance with validation.
    /// </summary>
    /// <param name="phoneNumber">Normalized phone number.</param>
    /// <param name="createdAt">UTC timestamp of creation.</param>
    /// <param name="role">Assigned role. Defaults to <see cref="UserRole.Customer"/>.</param>
    /// <param name="fullName">Optional full name.</param>
    /// <returns>A new <see cref="User"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public static User Create(
        string phoneNumber,
        DateTime createdAt,
        UserRole role = UserRole.Customer,
        string? fullName = null)
    {
        Validate(phoneNumber, fullName);

        return new User(phoneNumber.Trim(), createdAt, role, fullName);
    }

    /// <summary>
    /// Updates the user's full name.
    /// </summary>
    /// <param name="fullName">The new full name.</param>
    /// <exception cref="DomainException">Thrown when full name exceeds max length.</exception>
    public void UpdateName(string fullName)
    {
        if (fullName != null && fullName.Trim().Length > 100)
        {
            throw new DomainException("User full name cannot exceed 100 characters.");
        }

        FullName = fullName?.Trim() ?? string.Empty;
        Touch();
    }

    /// <summary>
    /// Alias for <see cref="UpdateName(string)"/> matching the gap remediation plan contract.
    /// </summary>
    /// <param name="name">The new full name.</param>
    public void SetName(string name) => UpdateName(name);

    /// <summary>
    /// Updates the user's role.
    /// </summary>
    /// <param name="newRole">The new user role.</param>
    public void UpdateRole(UserRole newRole)
    {
        Role = newRole;
        Touch();
    }

    /// <summary>
    /// Archives the user account (soft delete).
    /// </summary>
    /// <exception cref="DomainException">Thrown when the account is already archived.</exception>
    public void Archive()
    {
        if (!IsActive)
        {
            throw new DomainException("This user account is already archived.");
        }

        IsActive = false;
        Touch();
    }

    /// <summary>
    /// Reactivates an archived user account.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        Touch();
    }

    private static void Validate(string phoneNumber, string? fullName)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new DomainException("Phone number is required and cannot be empty.");
        }

        if (fullName != null && fullName.Trim().Length > 100)
        {
            throw new DomainException("User full name cannot exceed 100 characters.");
        }
    }
}
