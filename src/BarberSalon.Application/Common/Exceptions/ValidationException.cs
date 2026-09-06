namespace BarberSalon.Application.Common.Exceptions;

/// <summary>
/// Thrown when business input data fails validation at the application layer.
/// </summary>
public sealed class ValidationException : Exception
{
    /// <summary>Creates a validation exception with the given message.</summary>
    /// <param name="message">Human-readable description of the validation failure.</param>
    public ValidationException(string message) : base(message) { }
}