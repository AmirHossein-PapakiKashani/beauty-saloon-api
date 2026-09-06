using BarberSalon.Domain.Auth.Entities;
using BarberSalon.Domain.Auth.Enums;
using BarberSalon.Domain.Common;
using FluentAssertions;
using Xunit;

namespace BarberSalon.Domain.Tests;

public class UserTests
{
    private readonly DateTime _now = new(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_WithValidPhoneNumber_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var user = User.Create("09123456789", _now);

        // Assert
        user.Id.Should().NotBeEmpty();
        user.PhoneNumber.Should().Be("09123456789");
        user.FullName.Should().BeEmpty();
        user.Role.Should().Be(UserRole.Customer);
        user.IsActive.Should().BeTrue();
        user.CreatedAt.Should().Be(_now);
        user.UpdatedAt.Should().Be(_now);
    }

    [Fact]
    public void Create_WithCustomRoleAndName_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var user = User.Create("09123456789", _now, UserRole.Admin, "مدیر سالن");

        // Assert
        user.PhoneNumber.Should().Be("09123456789");
        user.FullName.Should().Be("مدیر سالن");
        user.Role.Should().Be(UserRole.Admin);
        user.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithInvalidPhoneNumber_ShouldThrowDomainException(string? invalidPhone)
    {
        // Act
        var act = () => User.Create(invalidPhone!, _now);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*phone*");
    }

    [Fact]
    public void Create_WithExcessiveFullNameLength_ShouldThrowDomainException()
    {
        // Arrange
        var longName = new string('a', 101);

        // Act
        var act = () => User.Create("09123456789", _now, UserRole.Customer, longName);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*name*");
    }

    [Fact]
    public void UpdateName_WithValidName_ShouldUpdateFullNameAndTouch()
    {
        // Arrange
        var user = User.Create("09123456789", _now);

        // Act
        user.UpdateName("علی رضایی");

        // Assert
        user.FullName.Should().Be("علی رضایی");
        user.UpdatedAt.Should().BeOnOrAfter(_now);
    }

    [Fact]
    public void UpdateRole_WithValidRole_ShouldUpdateRoleAndTouch()
    {
        // Arrange
        var user = User.Create("09123456789", _now);

        // Act
        user.UpdateRole(UserRole.Staff);

        // Assert
        user.Role.Should().Be(UserRole.Staff);
        user.UpdatedAt.Should().BeOnOrAfter(_now);
    }

    [Fact]
    public void Archive_WhenActive_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var user = User.Create("09123456789", _now);

        // Act
        user.Archive();

        // Assert
        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Archive_WhenAlreadyArchived_ShouldThrowDomainException()
    {
        // Arrange
        var user = User.Create("09123456789", _now);
        user.Archive();

        // Act
        var act = () => user.Archive();

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*archived*");
    }

    [Fact]
    public void Activate_WhenArchived_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var user = User.Create("09123456789", _now);
        user.Archive();

        // Act
        user.Activate();

        // Assert
        user.IsActive.Should().BeTrue();
    }
}
