namespace BarberSalon.API.Common;

/// <summary>
/// Standard response envelope returned by every endpoint.
/// </summary>
/// <typeparam name="T">Type of the payload.</typeparam>
/// <param name="Data">The endpoint payload.</param>
/// <param name="Success">Whether the request succeeded.</param>
/// <param name="Message">Optional human-readable message.</param>
public sealed record ApiResponse<T>(T Data, bool Success, string Message)
{
    /// <summary>Creates a success response wrapping the given payload.</summary>
    /// <param name="data">The payload to return.</param>
    /// <param name="message">Optional human-readable message.</param>
    public static ApiResponse<T> CreateSuccess(T data, string message = "") => new(data, true, message);
}