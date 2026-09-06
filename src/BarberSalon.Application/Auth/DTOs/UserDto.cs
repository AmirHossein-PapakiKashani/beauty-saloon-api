namespace BarberSalon.Application.Auth.DTOs;

/// <summary>
/// Data transfer object representing an authenticated user profile.
/// </summary>
/// <param name="Id">The unique user identifier.</param>
/// <param name="PhoneNumber">The user contact phone number.</param>
/// <param name="Name">The display full name.</param>
/// <param name="Role">The user's assigned role name (e.g. Customer, Staff, Admin).</param>
/// <param name="CreatedAt">The timestamp of user registration.</param>
public sealed record UserDto(
    Guid Id,
    string PhoneNumber,
    string Name,
    string Role,
    DateTime CreatedAt);
