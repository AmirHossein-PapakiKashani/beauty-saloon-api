namespace BarberSalon.Application.Auth.DTOs;

/// <summary>
/// Response payload for OTP verification containing authentication status, user profile, new user flag, and auth token.
/// </summary>
/// <param name="Success">Whether verification succeeded.</param>
/// <param name="User">Authenticated user details, or null if verification failed.</param>
/// <param name="IsNewUser">True if a new user record was created during this verification.</param>
/// <param name="Token">Authentication token string, or null if verification failed.</param>
public sealed record VerifyOtpResponse(
    bool Success,
    UserDto? User,
    bool IsNewUser,
    string? Token = null);

