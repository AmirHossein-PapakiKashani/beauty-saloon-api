using BarberSalon.Application.Auth.DTOs;
using BarberSalon.Application.Auth.Interfaces;
using BarberSalon.Application.Common.Exceptions;
using BarberSalon.Application.Common.Interfaces;
using BarberSalon.Domain.Auth.Entities;

namespace BarberSalon.Application.Auth.Services;

/// <summary>
/// Application service orchestrating User profile and account operations.
/// </summary>
public sealed class UserService
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of <see cref="UserService"/>.
    /// </summary>
    /// <param name="userRepository">Repository contract for users.</param>
    /// <param name="unitOfWork">Unit of work for persisting database changes.</param>
    public UserService(IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Returns a user profile by unique identifier.
    /// </summary>
    /// <param name="id">The user GUID identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user profile DTO.</returns>
    /// <exception cref="NotFoundException">Thrown when user is not found.</exception>
    public async Task<UserDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(User), id);

        return MapToDto(user);
    }

    /// <summary>
    /// Updates user profile details (such as display name).
    /// </summary>
    /// <param name="id">The user GUID identifier.</param>
    /// <param name="request">The update payload containing the new full name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated user profile DTO.</returns>
    /// <exception cref="ValidationException">Thrown when request payload is null.</exception>
    /// <exception cref="NotFoundException">Thrown when user is not found.</exception>
    public async Task<UserDto> UpdateProfileAsync(
        Guid id,
        UpdateUserProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ValidationException("Request body cannot be null.");
        }

        var user = await _userRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(User), id);

        if (request.Name is not null)
        {
            user.UpdateName(request.Name);
        }

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(user);
    }

    /// <summary>
    /// Maps a domain <see cref="User"/> entity to a <see cref="UserDto"/>.
    /// </summary>
    /// <param name="u">The user domain entity.</param>
    /// <returns>The mapped user DTO.</returns>
    public static UserDto MapToDto(User u) => new(
        u.Id,
        u.PhoneNumber,
        u.FullName,
        u.Role.ToString(),
        u.CreatedAt);
}
