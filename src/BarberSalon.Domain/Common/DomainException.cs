namespace BarberSalon.Domain.Common;

/// <summary>
/// Thrown when a domain invariant or business rule is violated inside an Entity or Value Object.
/// </summary>
public class DomainException : Exception
{
    /// <summary>Initializes a new instance of <see cref="DomainException"/>.</summary>
    /// <param name="message">The message that describes the error.</param>
    public DomainException(string message) : base(message) { }
}
