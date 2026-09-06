using System.Text.RegularExpressions;
using BarberSalon.Domain.Common;

namespace BarberSalon.Domain.Auth.Entities;

/// <summary>
/// Represents a one-time numeric password issued to a phone number for authentication.
/// </summary>
public sealed class OtpCode : BaseEntity
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);
    private static readonly Regex NumericCodeRegex = new(@"^\d{5}$", RegexOptions.Compiled);

    /// <summary>The recipient phone number.</summary>
    public string PhoneNumber { get; private set; } = string.Empty;

    /// <summary>The 5-digit verification code.</summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>UTC timestamp when this OTP expires.</summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>Whether this code has already been verified or invalidated.</summary>
    public bool IsUsed { get; private set; }

    /// <summary>Parameterless constructor for EF Core.</summary>
    private OtpCode() : base() { }

    private OtpCode(string phoneNumber, string code, DateTime createdAt, TimeSpan? ttl = null) : base(createdAt)
    {
        PhoneNumber = phoneNumber;
        Code = code;
        ExpiresAt = createdAt.Add(ttl ?? DefaultTtl);
        IsUsed = false;
    }

    /// <summary>
    /// Creates a new OTP entity after validating phone number and code format.
    /// </summary>
    /// <param name="phoneNumber">Recipient phone number.</param>
    /// <param name="code">5-digit numeric verification code.</param>
    /// <param name="createdAt">Current UTC timestamp.</param>
    /// <param name="ttl">Optional custom time-to-live. Defaults to 5 minutes.</param>
    /// <returns>A new <see cref="OtpCode"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when phone number or code is invalid.</exception>
    public static OtpCode Create(string phoneNumber, string code, DateTime createdAt, TimeSpan? ttl = null)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new DomainException("Phone number is required and cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(code) || !NumericCodeRegex.IsMatch(code))
        {
            throw new DomainException("OTP code must be exactly 5 digits.");
        }

        return new OtpCode(phoneNumber.Trim(), code, createdAt, ttl);
    }

    /// <summary>
    /// Marks the OTP as invalidated when a newer OTP is requested for the same phone number.
    /// </summary>
    public void Invalidate()
    {
        IsUsed = true;
        Touch();
    }

    /// <summary>
    /// Verifies and consumes the OTP.
    /// </summary>
    /// <param name="currentUtc">Current UTC timestamp.</param>
    /// <exception cref="DomainException">Thrown when the code has expired or is already used.</exception>
    public void MarkAsUsed(DateTime currentUtc)
    {
        if (IsUsed)
        {
            throw new DomainException("The OTP code has already been used.");
        }

        if (currentUtc > ExpiresAt)
        {
            throw new DomainException("The OTP code has expired.");
        }

        IsUsed = true;
        Touch();
    }

    /// <summary>
    /// Checks whether the OTP code is currently valid and active.
    /// </summary>
    /// <param name="currentUtc">Current UTC timestamp.</param>
    /// <returns>True if active and not expired, otherwise false.</returns>
    public bool IsValid(DateTime currentUtc) => !IsUsed && currentUtc <= ExpiresAt;
}
