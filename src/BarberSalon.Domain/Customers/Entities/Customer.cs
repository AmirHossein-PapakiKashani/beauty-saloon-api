using BarberSalon.Domain.Common;

namespace BarberSalon.Domain.Customers.Entities;

/// <summary>
/// Represents a customer who visits the salon to receive services.
/// Acts as an independent Aggregate Root in the Customers domain.
/// </summary>
public sealed class Customer : BaseEntity
{
    /// <summary>Full display name of the customer.</summary>
    public string FullName { get; private set; } = default!;

    /// <summary>Normalized contact phone number.</summary>
    public string PhoneNumber { get; private set; } = default!;

    /// <summary>Optional gender of the customer (e.g. "male", "female", "other").</summary>
    public string? Gender { get; private set; }

    /// <summary>Optional internal salon notes about the customer.</summary>
    public string Notes { get; private set; } = string.Empty;

    /// <summary>Optional linked User identity identifier for authentication.</summary>
    public Guid? UserId { get; private set; }

    /// <summary>Total count of completed or booked appointments.</summary>
    public int AppointmentsCount { get; private set; }

    /// <summary>Timestamp of the customer's last appointment, or null if none.</summary>
    public DateTime? LastAppointment { get; private set; }

    /// <summary>Whether the customer profile is active. False means archived (soft-deleted).</summary>
    public bool IsActive { get; private set; }

    // Private parameterless constructor required by EF Core
    private Customer() : base() { }

    private Customer(
        string fullName,
        string phoneNumber,
        string? gender,
        string? notes,
        Guid? userId,
        DateTime createdAt)
        : base(createdAt)
    {
        FullName = fullName.Trim();
        PhoneNumber = phoneNumber.Trim();
        Gender = gender?.Trim();
        Notes = notes?.Trim() ?? string.Empty;
        UserId = userId;
        AppointmentsCount = 0;
        LastAppointment = null;
        IsActive = true;
    }

    /// <summary>
    /// Creates a new <see cref="Customer"/> instance with validation.
    /// </summary>
    /// <param name="fullName">Customer full name. Required, max 100 characters.</param>
    /// <param name="phoneNumber">Customer phone number. Required, max 20 characters.</param>
    /// <param name="gender">Optional gender string.</param>
    /// <param name="notes">Optional notes. Max 1000 characters.</param>
    /// <param name="userId">Optional linked user ID.</param>
    /// <param name="createdAt">Optional explicit creation timestamp.</param>
    /// <returns>A new active <see cref="Customer"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when validation rules fail.</exception>
    public static Customer Create(
        string fullName,
        string phoneNumber,
        string? gender = null,
        string? notes = null,
        Guid? userId = null,
        DateTime? createdAt = null)
    {
        Validate(fullName, phoneNumber, gender, notes);

        return new Customer(
            fullName,
            phoneNumber,
            gender,
            notes,
            userId,
            createdAt ?? DateTime.UtcNow);
    }

    /// <summary>
    /// Updates customer profile details.
    /// </summary>
    /// <param name="fullName">Customer full name. Required, max 100 characters.</param>
    /// <param name="phoneNumber">Customer phone number. Required, max 20 characters.</param>
    /// <param name="gender">Optional gender string.</param>
    /// <param name="notes">Optional notes. Max 1000 characters.</param>
    /// <param name="userId">Optional linked user ID.</param>
    /// <exception cref="DomainException">Thrown when validation rules fail.</exception>
    public void Update(
        string fullName,
        string phoneNumber,
        string? gender = null,
        string? notes = null,
        Guid? userId = null)
    {
        Validate(fullName, phoneNumber, gender, notes);

        FullName = fullName.Trim();
        PhoneNumber = phoneNumber.Trim();
        Gender = gender?.Trim();
        Notes = notes?.Trim() ?? string.Empty;
        UserId = userId;
        Touch();
    }

    /// <summary>
    /// Records a new appointment for the customer, updating counts and last appointment timestamp.
    /// </summary>
    /// <param name="appointmentDate">The UTC timestamp of the appointment.</param>
    public void RecordAppointment(DateTime appointmentDate)
    {
        AppointmentsCount++;
        LastAppointment = appointmentDate;
        Touch();
    }

    /// <summary>
    /// Archives the customer profile (soft delete).
    /// </summary>
    /// <exception cref="DomainException">Thrown when customer is already archived.</exception>
    public void Archive()
    {
        if (!IsActive)
        {
            throw new DomainException("This customer is already archived.");
        }

        IsActive = false;
        Touch();
    }

    /// <summary>
    /// Reactivates an archived customer profile.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        Touch();
    }

    private static void Validate(string fullName, string phoneNumber, string? gender, string? notes)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new DomainException("Customer full name cannot be empty.");
        }

        if (fullName.Trim().Length > 100)
        {
            throw new DomainException("Customer full name cannot exceed 100 characters.");
        }

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new DomainException("Customer phone number cannot be empty.");
        }

        if (phoneNumber.Trim().Length > 20)
        {
            throw new DomainException("Customer phone number cannot exceed 20 characters.");
        }

        if (notes != null && notes.Trim().Length > 1000)
        {
            throw new DomainException("Customer notes cannot exceed 1000 characters.");
        }
    }
}
