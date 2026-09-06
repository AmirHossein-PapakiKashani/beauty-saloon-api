using BarberSalon.Domain.Common;
using BarberSalon.Domain.SalonServices.Entities;
using FluentAssertions;
using Xunit;

namespace BarberSalon.Domain.Tests.SalonServices;

public class SalonServiceTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldInitializeCorrectly()
    {
        // Act
        var service = SalonService.Create("Men's Haircut", "Professional haircut", 45, 150000m, "haircut");

        // Assert
        service.Id.Should().NotBeEmpty();
        service.Name.Should().Be("Men's Haircut");
        service.Description.Should().Be("Professional haircut");
        service.DurationMinutes.Should().Be(45);
        service.Price.Should().Be(150000m);
        service.Category.Should().Be("haircut");
        service.IsActive.Should().BeTrue();
        service.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        service.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Create_WithoutDescription_ShouldDefaultToEmptyString()
    {
        // Act
        var service = SalonService.Create("Beard Trim", 30, 80000m, "haircut");

        // Assert
        service.Description.Should().BeEmpty();
        service.Name.Should().Be("Beard Trim");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithInvalidName_ShouldThrowDomainException(string? invalidName)
    {
        // Act
        var act = () => SalonService.Create(invalidName!, "Desc", 30, 100000m, "haircut");

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*name*");
    }

    [Fact]
    public void Create_WithNameExceeding100Chars_ShouldThrowDomainException()
    {
        // Arrange
        var longName = new string('A', 101);

        // Act
        var act = () => SalonService.Create(longName, "Desc", 30, 100000m, "haircut");

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*100*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Create_WithNonPositiveDuration_ShouldThrowDomainException(int invalidDuration)
    {
        // Act
        var act = () => SalonService.Create("Haircut", "Desc", invalidDuration, 100000m, "haircut");

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Duration*");
    }

    [Fact]
    public void Create_WithDurationExceeding480Minutes_ShouldThrowDomainException()
    {
        // Act
        var act = () => SalonService.Create("Haircut", "Desc", 481, 100000m, "haircut");

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*480*");
    }

    [Fact]
    public void Create_WithNegativePrice_ShouldThrowDomainException()
    {
        // Act
        var act = () => SalonService.Create("Haircut", "Desc", 30, -1m, "haircut");

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*negative*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithInvalidCategory_ShouldThrowDomainException(string? invalidCategory)
    {
        // Act
        var act = () => SalonService.Create("Haircut", "Desc", 30, 100000m, invalidCategory!);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Category*");
    }

    [Fact]
    public void Update_WithValidParameters_ShouldUpdatePropertiesAndTouch()
    {
        // Arrange
        var service = SalonService.Create("Old Name", "Old Desc", 30, 100000m, "haircut");
        var initialUpdatedAt = service.UpdatedAt;

        // Act
        service.Update("New Name", "New Desc", 60, 200000m, "color");

        // Assert
        service.Name.Should().Be("New Name");
        service.Description.Should().Be("New Desc");
        service.DurationMinutes.Should().Be(60);
        service.Price.Should().Be(200000m);
        service.Category.Should().Be("color");
        service.UpdatedAt.Should().BeOnOrAfter(initialUpdatedAt);
    }

    [Fact]
    public void Archive_WhenActive_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var service = SalonService.Create("Haircut", 30, 100000m, "haircut");

        // Act
        service.Archive();

        // Assert
        service.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Archive_WhenAlreadyArchived_ShouldThrowDomainException()
    {
        // Arrange
        var service = SalonService.Create("Haircut", 30, 100000m, "haircut");
        service.Archive();

        // Act
        var act = () => service.Archive();

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*already archived*");
    }

    [Fact]
    public void Activate_WhenArchived_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var service = SalonService.Create("Haircut", 30, 100000m, "haircut");
        service.Archive();

        // Act
        service.Activate();

        // Assert
        service.IsActive.Should().BeTrue();
    }
}
