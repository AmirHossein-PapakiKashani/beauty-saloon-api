using BarberSalon.Domain.Common;
using BarberSalon.Domain.Salons.Entities;
using FluentAssertions;
using Xunit;

namespace BarberSalon.Domain.Tests.Salons;

public class SalonTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldInitializeCorrectly()
    {
        var now = DateTime.UtcNow;
        var ownerId = Guid.NewGuid();

        var salon = Salon.Create(
            "VIP Barber",
            "vip-barber",
            "02188889999",
            "Tehran, Valiasr St",
            now,
            ownerId,
            "Luxury salon branch");

        salon.Name.Should().Be("VIP Barber");
        salon.Slug.Should().Be("vip-barber");
        salon.PhoneNumber.Should().Be("02188889999");
        salon.Address.Should().Be("Tehran, Valiasr St");
        salon.OwnerUserId.Should().Be(ownerId);
        salon.Description.Should().Be("Luxury salon branch");
        salon.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "slug", "021", "addr")]
    [InlineData("Name", "", "021", "addr")]
    [InlineData("Name", "slug", "", "addr")]
    [InlineData("Name", "slug", "021", "")]
    public void Create_WithMissingRequiredFields_ShouldThrowDomainException(
        string name, string slug, string phone, string address)
    {
        var act = () => Salon.Create(name, slug, phone, address, DateTime.UtcNow);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Archive_WhenActive_ShouldSetIsActiveFalse()
    {
        var now = DateTime.UtcNow;
        var salon = Salon.Create("VIP Barber", "vip-barber", "02188889999", "Address", now);

        salon.Archive();

        salon.IsActive.Should().BeFalse();
        salon.UpdatedAt.Should().BeOnOrAfter(now);
    }

    [Fact]
    public void Archive_WhenAlreadyArchived_ShouldThrowDomainException()
    {
        var salon = Salon.Create("VIP Barber", "vip-barber", "02188889999", "Address", DateTime.UtcNow);
        salon.Archive();

        var act = () => salon.Archive();
        act.Should().Throw<DomainException>().WithMessage("This salon is already archived.");
    }

    [Fact]
    public void Activate_WhenArchived_ShouldSetIsActiveTrue()
    {
        var salon = Salon.Create("VIP Barber", "vip-barber", "02188889999", "Address", DateTime.UtcNow);
        salon.Archive();

        salon.Activate();

        salon.IsActive.Should().BeTrue();
    }

    [Fact]
    public void UpdateDetails_WithValidData_ShouldUpdatePropertiesAndTouch()
    {
        var now = DateTime.UtcNow;
        var salon = Salon.Create("Old Name", "old-slug", "021111", "Old Address", now);

        salon.UpdateDetails("New Name", "021222", "New Address", "New Desc");

        salon.Name.Should().Be("New Name");
        salon.PhoneNumber.Should().Be("021222");
        salon.Address.Should().Be("New Address");
        salon.Description.Should().Be("New Desc");
        salon.UpdatedAt.Should().BeOnOrAfter(now);
    }
}
