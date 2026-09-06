using BarberSalon.Domain.Auth.Entities;
using BarberSalon.Domain.Common;
using FluentAssertions;
using Xunit;

namespace BarberSalon.Domain.Tests;

public class OtpCodeTests
{
    private readonly DateTime _now = new(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_WithValidParameters_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var otp = OtpCode.Create("09123456789", "12345", _now);

        // Assert
        otp.PhoneNumber.Should().Be("09123456789");
        otp.Code.Should().Be("12345");
        otp.CreatedAt.Should().Be(_now);
        otp.ExpiresAt.Should().Be(_now.AddMinutes(5));
        otp.IsUsed.Should().BeFalse();
        otp.IsValid(_now).Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithInvalidPhoneNumber_ShouldThrowDomainException(string? invalidPhone)
    {
        // Act
        var act = () => OtpCode.Create(invalidPhone!, "12345", _now);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*phone*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("1234")]
    [InlineData("123456")]
    [InlineData("1234a")]
    [InlineData(null)]
    public void Create_WithInvalidCode_ShouldThrowDomainException(string? invalidCode)
    {
        // Act
        var act = () => OtpCode.Create("09123456789", invalidCode!, _now);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*code*");
    }

    [Fact]
    public void Invalidate_ShouldSetIsUsedToTrue()
    {
        // Arrange
        var otp = OtpCode.Create("09123456789", "12345", _now);

        // Act
        otp.Invalidate();

        // Assert
        otp.IsUsed.Should().BeTrue();
        otp.IsValid(_now).Should().BeFalse();
    }

    [Fact]
    public void IsValid_WhenExpired_ShouldReturnFalse()
    {
        // Arrange
        var otp = OtpCode.Create("09123456789", "12345", _now);

        // Act & Assert
        otp.IsValid(_now.AddMinutes(5).AddSeconds(1)).Should().BeFalse();
    }

    [Fact]
    public void MarkAsUsed_WhenActive_ShouldSetIsUsedToTrue()
    {
        // Arrange
        var otp = OtpCode.Create("09123456789", "12345", _now);

        // Act
        otp.MarkAsUsed(_now.AddMinutes(1));

        // Assert
        otp.IsUsed.Should().BeTrue();
        otp.IsValid(_now.AddMinutes(1)).Should().BeFalse();
    }

    [Fact]
    public void MarkAsUsed_WhenExpired_ShouldThrowDomainException()
    {
        // Arrange
        var otp = OtpCode.Create("09123456789", "12345", _now);

        // Act
        var act = () => otp.MarkAsUsed(_now.AddMinutes(6));

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*expired*");
    }

    [Fact]
    public void MarkAsUsed_WhenAlreadyUsed_ShouldThrowDomainException()
    {
        // Arrange
        var otp = OtpCode.Create("09123456789", "12345", _now);
        otp.MarkAsUsed(_now.AddMinutes(1));

        // Act
        var act = () => otp.MarkAsUsed(_now.AddMinutes(2));

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*used*");
    }
}
