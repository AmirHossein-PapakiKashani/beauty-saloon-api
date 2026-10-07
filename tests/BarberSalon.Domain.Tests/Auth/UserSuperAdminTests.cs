using BarberSalon.Domain.Auth.Entities;
using BarberSalon.Domain.Auth.Enums;
using FluentAssertions;
using Xunit;

namespace BarberSalon.Domain.Tests.Auth;

public class UserSuperAdminTests
{
    [Fact]
    public void UserRole_ShouldContainSuperAdminValue()
    {
        // Assert
        ((int)UserRole.SuperAdmin).Should().Be(4);
    }

    [Fact]
    public void Create_WithSuperAdminRole_ShouldInitializeCorrectly()
    {
        // Arrange
        var now = DateTime.UtcNow;

        // Act
        var user = User.Create("09121112233", now, UserRole.SuperAdmin, "TechFlow Admin");

        // Assert
        user.Role.Should().Be(UserRole.SuperAdmin);
        user.FullName.Should().Be("TechFlow Admin");
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void TouchActive_ShouldUpdateLastActiveAtAndTouch()
    {
        // Arrange
        var created = DateTime.UtcNow.AddHours(-1);
        var user = User.Create("09121112233", created, UserRole.SuperAdmin, "TechFlow Admin");
        var activeTime = DateTime.UtcNow;

        // Act
        user.TouchActive(activeTime);

        // Assert
        user.LastActiveAt.Should().Be(activeTime);
        user.UpdatedAt.Should().BeOnOrAfter(created);
    }
}
