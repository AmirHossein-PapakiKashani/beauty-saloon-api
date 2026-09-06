using BarberSalon.Application.Common.Exceptions;
using BarberSalon.Application.Common.Interfaces;
using BarberSalon.Application.Customers.DTOs;
using BarberSalon.Application.Customers.Interfaces;
using BarberSalon.Application.Customers.Services;
using BarberSalon.Domain.Customers.Entities;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace BarberSalon.Application.Tests.Customers;

public class CustomerServiceTests
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CustomerService _service;

    public CustomerServiceTests()
    {
        _customerRepository = Substitute.For<ICustomerRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _service = new CustomerService(_customerRepository, _unitOfWork);
    }

    [Fact]
    public async Task CreateCustomerAsync_WithValidRequest_AddsCustomerAndReturnsDto()
    {
        // Arrange
        var request = new CreateCustomerRequest("سارا محمدی", "09121112233", "female", "یادداشت مشتری");
        _customerRepository.ExistsByPhoneNumberAsync("09121112233", Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _service.CreateCustomerAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Name.Should().Be("سارا محمدی");
        result.Phone.Should().Be("09121112233");
        result.Gender.Should().Be("female");
        result.Notes.Should().Be("یادداشت مشتری");
        result.Status.Should().Be("active");
        result.AppointmentsCount.Should().Be(0);
        result.LastAppointment.Should().BeNull();

        await _customerRepository.Received(1).AddAsync(Arg.Is<Customer>(c =>
            c.FullName == "سارا محمدی" &&
            c.PhoneNumber == "09121112233" &&
            c.Gender == "female" &&
            c.Notes == "یادداشت مشتری"
        ), Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateCustomerAsync_WithNullRequest_ThrowsValidationException()
    {
        // Act
        var act = () => _service.CreateCustomerAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*null*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateCustomerAsync_WithEmptyName_ThrowsValidationException(string? invalidName)
    {
        // Arrange
        var request = new CreateCustomerRequest(invalidName!, "09121112233");

        // Act
        var act = () => _service.CreateCustomerAsync(request);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*name*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateCustomerAsync_WithEmptyPhone_ThrowsValidationException(string? invalidPhone)
    {
        // Arrange
        var request = new CreateCustomerRequest("سارا محمدی", invalidPhone!);

        // Act
        var act = () => _service.CreateCustomerAsync(request);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*phone*");
    }

    [Fact]
    public async Task CreateCustomerAsync_WhenPhoneAlreadyExists_ThrowsValidationException()
    {
        // Arrange
        var request = new CreateCustomerRequest("سارا محمدی", "09121112233");
        _customerRepository.ExistsByPhoneNumberAsync("09121112233", Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var act = () => _service.CreateCustomerAsync(request);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*already exists*");

        await _customerRepository.DidNotReceive().AddAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_WhenCustomerExists_ReturnsCustomerDto()
    {
        // Arrange
        var customer = Customer.Create("مریم رضایی", "09123334455", "female", "یادداشت تست");
        _customerRepository.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>())
            .Returns(customer);

        // Act
        var result = await _service.GetByIdAsync(customer.Id);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(customer.Id);
        result.Name.Should().Be("مریم رضایی");
        result.Phone.Should().Be("09123334455");
        result.Gender.Should().Be("female");
        result.Notes.Should().Be("یادداشت تست");
        result.Status.Should().Be("active");
    }

    [Fact]
    public async Task GetByIdAsync_WhenCustomerDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        _customerRepository.GetByIdAsync(customerId, Arg.Any<CancellationToken>())
            .Returns((Customer?)null);

        // Act
        var act = () => _service.GetByIdAsync(customerId);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*{nameof(Customer)}*{customerId}*");
    }

    [Fact]
    public async Task GetCustomersAsync_WhenNoStatusFilter_ReturnsAllCustomersMappedToDto()
    {
        // Arrange
        var c1 = Customer.Create("علی احمدی", "09121111111");
        var c2 = Customer.Create("زهرا رضایی", "09122222222");
        c2.Archive();

        _customerRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Customer> { c1, c2 });

        // Act
        var result = await _service.GetCustomersAsync();

        // Assert
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("علی احمدی");
        result[0].Status.Should().Be("active");
        result[1].Name.Should().Be("زهرا رضایی");
        result[1].Status.Should().Be("inactive");
        await _customerRepository.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCustomersAsync_WhenStatusIsActive_ReturnsOnlyActiveCustomers()
    {
        // Arrange
        var c1 = Customer.Create("علی احمدی", "09121111111");

        _customerRepository.GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Customer> { c1 });

        // Act
        var result = await _service.GetCustomersAsync("active");

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("علی احمدی");
        result[0].Status.Should().Be("active");
        await _customerRepository.Received(1).GetAllActiveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCustomersAsync_WhenStatusIsInactive_ReturnsOnlyInactiveCustomers()
    {
        // Arrange
        var c1 = Customer.Create("علی احمدی", "09121111111");
        var c2 = Customer.Create("زهرا رضایی", "09122222222");
        c2.Archive();

        _customerRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Customer> { c1, c2 });

        // Act
        var result = await _service.GetCustomersAsync("inactive");

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("زهرا رضایی");
        result[0].Status.Should().Be("inactive");
        await _customerRepository.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCustomersAsync_WhenRepositoryReturnsEmpty_ReturnsEmptyList()
    {
        // Arrange
        _customerRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Customer>());

        // Act
        var result = await _service.GetCustomersAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllActiveAsync_ReturnsActiveCustomers()
    {
        // Arrange
        var c1 = Customer.Create("علی احمدی", "09121111111");
        _customerRepository.GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Customer> { c1 });

        // Act
        var result = await _service.GetAllActiveAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("علی احمدی");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllCustomers()
    {
        // Arrange
        var c1 = Customer.Create("علی احمدی", "09121111111");
        var c2 = Customer.Create("زهرا رضایی", "09122222222");
        _customerRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Customer> { c1, c2 });

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchCustomersAsync_WithMatchingName_ReturnsMatchingCustomersMappedToDto()
    {
        // Arrange
        var c1 = Customer.Create("کیوان رضایی", "09121112233");
        _customerRepository.SearchAsync("رضایی", Arg.Any<CancellationToken>())
            .Returns(new List<Customer> { c1 });

        // Act
        var result = await _service.SearchCustomersAsync("رضایی");

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("کیوان رضایی");
        result[0].Phone.Should().Be("09121112233");
        result[0].Status.Should().Be("active");
        await _customerRepository.Received(1).SearchAsync("رضایی", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchCustomersAsync_WithMatchingPhone_ReturnsMatchingCustomersMappedToDto()
    {
        // Arrange
        var c1 = Customer.Create("کیوان رضایی", "09121112233");
        _customerRepository.SearchAsync("0912111", Arg.Any<CancellationToken>())
            .Returns(new List<Customer> { c1 });

        // Act
        var result = await _service.SearchCustomersAsync("0912111");

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("کیوان رضایی");
        result[0].Phone.Should().Be("09121112233");
        await _customerRepository.Received(1).SearchAsync("0912111", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchCustomersAsync_WhenNoMatchFound_ReturnsEmptyList()
    {
        // Arrange
        _customerRepository.SearchAsync("ناموجود", Arg.Any<CancellationToken>())
            .Returns(new List<Customer>());

        // Act
        var result = await _service.SearchCustomersAsync("ناموجود");

        // Assert
        result.Should().BeEmpty();
        await _customerRepository.Received(1).SearchAsync("ناموجود", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchCustomersAsync_WhenQueryIsNull_ReturnsAllActiveCustomers()
    {
        // Arrange
        var c1 = Customer.Create("علی احمدی", "09121111111");
        _customerRepository.GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Customer> { c1 });

        // Act
        var result = await _service.SearchCustomersAsync(null);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("علی احمدی");
        await _customerRepository.Received(1).GetAllActiveAsync(Arg.Any<CancellationToken>());
        await _customerRepository.DidNotReceive().SearchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SearchCustomersAsync_WhenQueryIsEmptyOrWhitespace_ReturnsAllActiveCustomers(string? query)
    {
        // Arrange
        var c1 = Customer.Create("علی احمدی", "09121111111");
        _customerRepository.GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Customer> { c1 });

        // Act
        var result = await _service.SearchCustomersAsync(query);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("علی احمدی");
        await _customerRepository.Received(1).GetAllActiveAsync(Arg.Any<CancellationToken>());
        await _customerRepository.DidNotReceive().SearchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchCustomersAsync_TrimsQueryStringBeforeCallingRepository()
    {
        // Arrange
        var c1 = Customer.Create("کیوان رضایی", "09121112233");
        _customerRepository.SearchAsync("رضایی", Arg.Any<CancellationToken>())
            .Returns(new List<Customer> { c1 });

        // Act
        var result = await _service.SearchCustomersAsync("   رضایی   ");

        // Assert
        result.Should().HaveCount(1);
        await _customerRepository.Received(1).SearchAsync("رضایی", Arg.Any<CancellationToken>());
    }
}
