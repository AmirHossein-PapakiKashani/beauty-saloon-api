using BarberSalon.Domain.Common;
using BarberSalon.Domain.Customers.Entities;
using FluentAssertions;
using Xunit;

namespace BarberSalon.Domain.Tests.Customers;

public class CustomerTests
{
    [Fact]
    public void Create_WithValidData_ReturnsActiveCustomer()
    {
        // Arrange
        var fullName = "مریم حسینی";
        var phone = "09121112233";
        var gender = "female";
        var notes = "مشتری VIP";

        // Act
        var customer = Customer.Create(fullName, phone, gender, notes);

        // Assert
        customer.Should().NotBeNull();
        customer.Id.Should().NotBeEmpty();
        customer.FullName.Should().Be("مریم حسینی");
        customer.PhoneNumber.Should().Be("09121112233");
        customer.Gender.Should().Be("female");
        customer.Notes.Should().Be("مشتری VIP");
        customer.IsActive.Should().BeTrue();
        customer.AppointmentsCount.Should().Be(0);
        customer.LastAppointment.Should().BeNull();
        customer.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        customer.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Create_TrimsWhitespaceFromFields()
    {
        // Act
        var customer = Customer.Create("  سارا راد  ", "  09123334455  ", " female ", "  یادداشت  ");

        // Assert
        customer.FullName.Should().Be("سارا راد");
        customer.PhoneNumber.Should().Be("09123334455");
        customer.Gender.Should().Be("female");
        customer.Notes.Should().Be("یادداشت");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidFullName_ThrowsDomainException(string? invalidName)
    {
        // Act
        var act = () => Customer.Create(invalidName!, "09121112233");

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*full name*");
    }

    [Fact]
    public void Create_WithFullNameExceeding100Chars_ThrowsDomainException()
    {
        // Arrange
        var longName = new string('A', 101);

        // Act
        var act = () => Customer.Create(longName, "09121112233");

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*100 characters*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidPhoneNumber_ThrowsDomainException(string? invalidPhone)
    {
        // Act
        var act = () => Customer.Create("زهرا رضایی", invalidPhone!);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*phone number*");
    }

    [Fact]
    public void Create_WithPhoneNumberExceeding20Chars_ThrowsDomainException()
    {
        // Arrange
        var longPhone = new string('1', 21);

        // Act
        var act = () => Customer.Create("زهرا رضایی", longPhone);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*20 characters*");
    }

    [Fact]
    public void Create_WithNotesExceeding1000Chars_ThrowsDomainException()
    {
        // Arrange
        var longNotes = new string('N', 1001);

        // Act
        var act = () => Customer.Create("زهرا رضایی", "09121112233", notes: longNotes);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*1000 characters*");
    }

    [Fact]
    public void Update_WithValidData_UpdatesFieldsAndTouchesTimestamp()
    {
        // Arrange
        var customer = Customer.Create("ندا کریمی", "09125556677", "female", "یادداشت اولیه");
        var initialUpdatedAt = customer.UpdatedAt;

        // Act
        customer.Update("ندا حسینی", "09125556688", "female", "یادداشت جدید");

        // Assert
        customer.FullName.Should().Be("ندا حسینی");
        customer.PhoneNumber.Should().Be("09125556688");
        customer.Notes.Should().Be("یادداشت جدید");
        customer.UpdatedAt.Should().BeOnOrAfter(initialUpdatedAt);
    }

    [Fact]
    public void Archive_ActiveCustomer_SetsIsActiveFalse()
    {
        // Arrange
        var customer = Customer.Create("مینا مرادی", "09127778899");

        // Act
        customer.Archive();

        // Assert
        customer.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Archive_AlreadyArchivedCustomer_ThrowsDomainException()
    {
        // Arrange
        var customer = Customer.Create("مینا مرادی", "09127778899");
        customer.Archive();

        // Act
        var act = () => customer.Archive();

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*already archived*");
    }

    [Fact]
    public void Activate_ArchivedCustomer_SetsIsActiveTrue()
    {
        // Arrange
        var customer = Customer.Create("مینا مرادی", "09127778899");
        customer.Archive();

        // Act
        customer.Activate();

        // Assert
        customer.IsActive.Should().BeTrue();
    }

    [Fact]
    public void RecordAppointment_IncrementsCountAndSetsLastAppointmentDate()
    {
        // Arrange
        var customer = Customer.Create("لیلا نوری", "09129990011");
        var aptDate = new DateTime(2026, 9, 10, 14, 0, 0, DateTimeKind.Utc);

        // Act
        customer.RecordAppointment(aptDate);

        // Assert
        customer.AppointmentsCount.Should().Be(1);
        customer.LastAppointment.Should().Be(aptDate);

        // Act 2
        var nextDate = new DateTime(2026, 9, 20, 16, 0, 0, DateTimeKind.Utc);
        customer.RecordAppointment(nextDate);

        // Assert 2
        customer.AppointmentsCount.Should().Be(2);
        customer.LastAppointment.Should().Be(nextDate);
    }
}
