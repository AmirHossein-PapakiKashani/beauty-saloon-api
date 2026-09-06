namespace BarberSalon.Application.Common.Exceptions;

/// <summary>Thrown when a requested entity does not exist in the database.</summary>
public sealed class NotFoundException : Exception
{
    /// <summary>Initializes a new instance of <see cref="NotFoundException"/>.</summary>
    /// <param name="entityName">The name of the entity that was not found.</param>
    /// <param name="key">The key or identifier that was searched for.</param>
    public NotFoundException(string entityName, object key)
        : base($"{entityName} with key '{key}' was not found.") { }
}
