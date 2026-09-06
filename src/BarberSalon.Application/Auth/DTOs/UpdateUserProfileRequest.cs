namespace BarberSalon.Application.Auth.DTOs;

/// <summary>
/// Input model for updating a user's profile information.
/// </summary>
/// <param name="Name">The updated full display name.</param>
public sealed record UpdateUserProfileRequest(string? Name);
