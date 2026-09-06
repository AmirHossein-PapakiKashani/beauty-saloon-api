using BarberSalon.Application.Auth.DTOs;
using BarberSalon.Application.Auth.Interfaces;
using BarberSalon.Application.Auth.Services;
using BarberSalon.Application.Common.Exceptions;
using BarberSalon.Application.Common.Interfaces;
using BarberSalon.Domain.Auth.Entities;
using BarberSalon.Domain.Auth.Enums;
using BarberSalon.Domain.Common;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace BarberSalon.Application.Tests.Auth;

public class UserServiceTests
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserService _sut;

    private readonly DateTime _utcNow = new(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);

    public UserServiceTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        _sut = new UserService(_userRepository, _unitOfWork);
    }

    [Fact]
    public async Task UpdateProfileAsync_WithValidName_UpdatesFullName_CallsUpdate_SavesChanges_AndReturnsDto()
    {
        // Arrange
        var user = User.Create("09121112233", _utcNow);
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(user);

        var request = new UpdateUserProfileRequest("سارا محمدی");

        // Act
        var result = await _sut.UpdateProfileAsync(user.Id, request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(user.Id);
        result.PhoneNumber.Should().Be("09121112233");
        result.Name.Should().Be("سارا محمدی");
        result.Role.Should().Be("Customer");

        user.FullName.Should().Be("سارا محمدی");
        _userRepository.Received(1).Update(user);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateProfileAsync_WithWhitespaceSurroundingName_TrimsName()
    {
        // Arrange
        var user = User.Create("09121112233", _utcNow);
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(user);

        var request = new UpdateUserProfileRequest("   علی حسینی   ");

        // Act
        var result = await _sut.UpdateProfileAsync(user.Id, request);

        // Assert
        result.Name.Should().Be("علی حسینی");
        user.FullName.Should().Be("علی حسینی");
    }

    [Fact]
    public async Task UpdateProfileAsync_WhenUserNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _userRepository.GetByIdAsync(nonExistentId, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var request = new UpdateUserProfileRequest("سارا محمدی");

        // Act
        var act = () => _sut.UpdateProfileAsync(nonExistentId, request);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*{nonExistentId}*");

        _userRepository.DidNotReceive().Update(Arg.Any<User>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateProfileAsync_WithNullRequest_ThrowsValidationException()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var act = () => _sut.UpdateProfileAsync(userId, null!);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Request body cannot be null*");
    }

    [Fact]
    public async Task UpdateProfileAsync_WithExcessiveNameLength_ThrowsDomainException()
    {
        // Arrange
        var user = User.Create("09121112233", _utcNow);
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(user);

        var longName = new string('x', 101);
        var request = new UpdateUserProfileRequest(longName);

        // Act
        var act = () => _sut.UpdateProfileAsync(user.Id, request);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*100 characters*");
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserExists_ReturnsUserDto()
    {
        // Arrange
        var user = User.Create("09123334455", _utcNow, UserRole.Admin, "مدیر سالن");
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(user);

        // Act
        var result = await _sut.GetByIdAsync(user.Id);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(user.Id);
        result.PhoneNumber.Should().Be("09123334455");
        result.Name.Should().Be("مدیر سالن");
        result.Role.Should().Be("Admin");
        result.CreatedAt.Should().Be(_utcNow);
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _userRepository.GetByIdAsync(nonExistentId, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        // Act
        var act = () => _sut.GetByIdAsync(nonExistentId);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*{nonExistentId}*");
    }
}
