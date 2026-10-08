using BarberSalon.Domain.Common;
using BarberSalon.Domain.Staff.Entities;
using FluentAssertions;
using Xunit;

namespace BarberSalon.Domain.Tests.Staff;

public class StaffMemberTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldInitializeCorrectly()
    {
        // Arrange
        var serviceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var specialties = new List<string> { "fade", "خط‌کشی" };
        var serviceIds = new List<Guid> { serviceId };

        // Act
        var staff = StaffMember.Create(
            "آقای کریمی",
            "karimi",
            "09121234567",
            "آرایشگر با ۱۰ سال تجربه",
            "آرایشگر ارشد",
            10,
            specialties,
            serviceIds,
            userId,
            "{}"
        );

        // Assert
        staff.Id.Should().NotBeEmpty();
        staff.FullName.Should().Be("آقای کریمی");
        staff.Slug.Should().Be("karimi");
        staff.PhoneNumber.Should().Be("09121234567");
        staff.Bio.Should().Be("آرایشگر با ۱۰ سال تجربه");
        staff.Role.Should().Be("آرایشگر ارشد");
        staff.YearsExperience.Should().Be(10);
        staff.IsActive.Should().BeTrue();
        staff.Specialties.Should().ContainInOrder("fade", "خط‌کشی");
        staff.ServiceIds.Should().Contain(serviceId);
        staff.UserId.Should().Be(userId);
        staff.WorkingHoursJson.Should().Be("{}");
        staff.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        staff.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithInvalidFullName_ShouldThrowDomainException(string? invalidName)
    {
        // Act
        var act = () => StaffMember.Create(invalidName!, "karimi", "09121234567", "Bio", "Barber", 5);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*full name*");
    }

    [Fact]
    public void Create_WithFullNameExceeding100Chars_ShouldThrowDomainException()
    {
        // Arrange
        var longName = new string('A', 101);

        // Act
        var act = () => StaffMember.Create(longName, "karimi", "09121234567", "Bio", "Barber", 5);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*100*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithInvalidSlug_ShouldThrowDomainException(string? invalidSlug)
    {
        // Act
        var act = () => StaffMember.Create("Ali Karimi", invalidSlug!, "09121234567", "Bio", "Barber", 5);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*slug*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithInvalidPhoneNumber_ShouldThrowDomainException(string? invalidPhone)
    {
        // Act
        var act = () => StaffMember.Create("Ali Karimi", "karimi", invalidPhone!, "Bio", "Barber", 5);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*phone number*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithInvalidRole_ShouldThrowDomainException(string? invalidRole)
    {
        // Act
        var act = () => StaffMember.Create("Ali Karimi", "karimi", "09121234567", "Bio", invalidRole!, 5);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*role*");
    }

    [Fact]
    public void Create_WithNegativeYearsExperience_ShouldThrowDomainException()
    {
        // Act
        var act = () => StaffMember.Create("Ali Karimi", "karimi", "09121234567", "Bio", "Barber", -1);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*experience*");
    }

    [Fact]
    public void Update_WithValidParameters_ShouldUpdatePropertiesAndTouch()
    {
        // Arrange
        var staff = StaffMember.Create("Old Name", "old-slug", "09120000000", "Old Bio", "Barber", 3);
        var initialUpdatedAt = staff.UpdatedAt;

        // Act
        staff.Update("New Name", "new-slug", "09121111111", "New Bio", "Senior Barber", 5);

        // Assert
        staff.FullName.Should().Be("New Name");
        staff.Slug.Should().Be("new-slug");
        staff.PhoneNumber.Should().Be("09121111111");
        staff.Bio.Should().Be("New Bio");
        staff.Role.Should().Be("Senior Barber");
        staff.YearsExperience.Should().Be(5);
        staff.UpdatedAt.Should().BeOnOrAfter(initialUpdatedAt);
    }

    [Fact]
    public void Archive_WhenActive_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var staff = StaffMember.Create("Ali Karimi", "karimi", "09121234567", "Bio", "Barber", 5);

        // Act
        staff.Archive();

        // Assert
        staff.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Archive_WhenAlreadyArchived_ShouldThrowDomainException()
    {
        // Arrange
        var staff = StaffMember.Create("Ali Karimi", "karimi", "09121234567", "Bio", "Barber", 5);
        staff.Archive();

        // Act
        var act = () => staff.Archive();

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*already archived*");
    }

    [Fact]
    public void Activate_WhenArchived_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var staff = StaffMember.Create("Ali Karimi", "karimi", "09121234567", "Bio", "Barber", 5);
        staff.Archive();

        // Act
        staff.Activate();

        // Assert
        staff.IsActive.Should().BeTrue();
    }

    [Fact]
    public void AssignService_WhenNotAssigned_ShouldAddServiceId()
    {
        // Arrange
        var staff = StaffMember.Create("Ali Karimi", "karimi", "09121234567", "Bio", "Barber", 5);
        var serviceId = Guid.NewGuid();

        // Act
        staff.AssignService(serviceId);

        // Assert
        staff.ServiceIds.Should().Contain(serviceId);
    }

    [Fact]
    public void AssignService_WhenAlreadyAssigned_ShouldNotDuplicate()
    {
        // Arrange
        var serviceId = Guid.NewGuid();
        var staff = StaffMember.Create("Ali Karimi", "karimi", "09121234567", "Bio", "Barber", 5, serviceIds: new List<Guid> { serviceId });

        // Act
        staff.AssignService(serviceId);

        // Assert
        staff.ServiceIds.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveService_WhenAssigned_ShouldRemoveServiceId()
    {
        // Arrange
        var serviceId = Guid.NewGuid();
        var staff = StaffMember.Create("Ali Karimi", "karimi", "09121234567", "Bio", "Barber", 5, serviceIds: new List<Guid> { serviceId });

        // Act
        staff.RemoveService(serviceId);

        // Assert
        staff.ServiceIds.Should().NotContain(serviceId);
    }

    [Fact]
    public void Create_WithServicePrices_InitializesCorrectly()
    {
        // Arrange
        var serviceId = Guid.NewGuid();
        var prices = new Dictionary<Guid, decimal> { [serviceId] = 200000m };

        // Act
        var staff = StaffMember.Create(
            "خانم احمدی",
            "ahmadi",
            "09129876543",
            "متخصص رنگ",
            "متخصص رنگ",
            8,
            serviceIds: new List<Guid> { serviceId },
            servicePrices: prices
        );

        // Assert
        staff.ServicePrices.Should().ContainKey(serviceId);
        staff.ServicePrices[serviceId].Should().Be(200000m);
    }

    [Fact]
    public void SetServicePrice_WithValidPrice_SetsPrice()
    {
        // Arrange
        var serviceId = Guid.NewGuid();
        var staff = StaffMember.Create("Ali Karimi", "karimi", "09121234567", "Bio", "Barber", 5);

        // Act
        staff.SetServicePrice(serviceId, 180000m);

        // Assert
        staff.ServicePrices.Should().ContainKey(serviceId);
        staff.ServicePrices[serviceId].Should().Be(180000m);
        staff.ServiceIds.Should().Contain(serviceId);
    }

    [Fact]
    public void SetServicePrice_WithNegativePrice_ThrowsDomainException()
    {
        // Arrange
        var serviceId = Guid.NewGuid();
        var staff = StaffMember.Create("Ali Karimi", "karimi", "09121234567", "Bio", "Barber", 5);

        // Act
        var act = () => staff.SetServicePrice(serviceId, -100m);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Price cannot be negative*");
    }

    [Fact]
    public void GetServicePrice_WhenCustomPriceExists_ReturnsCustomPrice()
    {
        // Arrange
        var serviceId = Guid.NewGuid();
        var staff = StaffMember.Create("Ali Karimi", "karimi", "09121234567", "Bio", "Barber", 5);
        staff.SetServicePrice(serviceId, 250000m);

        // Act
        var price = staff.GetServicePrice(serviceId, defaultPrice: 150000m);

        // Assert
        price.Should().Be(250000m);
    }

    [Fact]
    public void GetServicePrice_WhenNoCustomPriceExists_ReturnsDefaultPrice()
    {
        // Arrange
        var serviceId = Guid.NewGuid();
        var staff = StaffMember.Create("Ali Karimi", "karimi", "09121234567", "Bio", "Barber", 5);

        // Act
        var price = staff.GetServicePrice(serviceId, defaultPrice: 150000m);

        // Assert
        price.Should().Be(150000m);
    }

    [Fact]
    public void RemoveService_WhenCustomPriceExists_RemovesCustomPriceToo()
    {
        // Arrange
        var serviceId = Guid.NewGuid();
        var staff = StaffMember.Create("Ali Karimi", "karimi", "09121234567", "Bio", "Barber", 5, serviceIds: new List<Guid> { serviceId });
        staff.SetServicePrice(serviceId, 180000m);

        // Act
        staff.RemoveService(serviceId);

        // Assert
        staff.ServiceIds.Should().NotContain(serviceId);
        staff.ServicePrices.Should().NotContainKey(serviceId);
    }
}
