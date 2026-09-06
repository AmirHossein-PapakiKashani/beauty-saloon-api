using BarberSalon.Application.Common.Exceptions;
using BarberSalon.Application.Common.Interfaces;
using BarberSalon.Application.SalonServices.DTOs;
using BarberSalon.Application.SalonServices.Interfaces;
using BarberSalon.Application.SalonServices.Services;
using BarberSalon.Domain.SalonServices.Entities;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace BarberSalon.Application.Tests.SalonServices;

public class SalonServiceManagerTests
{
    private readonly ISalonServiceRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly SalonServiceManager _manager;

    public SalonServiceManagerTests()
    {
        _repository = Substitute.For<ISalonServiceRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _manager = new SalonServiceManager(_repository, _unitOfWork);
    }

    [Fact]
    public async Task GetAllActiveAsync_WhenServicesExist_ReturnsMappedDtos()
    {
        // Arrange
        var services = new List<SalonService>
        {
            SalonService.Create("Men's Haircut", "Haircut", 30, 150000m, "haircut"),
            SalonService.Create("Beard Trim", "Beard", 20, 80000m, "haircut")
        };
        _repository.GetAllActiveAsync(Arg.Any<CancellationToken>()).Returns(services);

        // Act
        var result = await _manager.GetAllActiveAsync(CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Men's Haircut");
        result[0].DurationMinutes.Should().Be(30);
        result[0].Price.Should().Be(150000m);
        result[0].Category.Should().Be("haircut");
        result[0].IsActive.Should().BeTrue();
        result[1].Name.Should().Be("Beard Trim");
    }

    [Fact]
    public async Task GetAllActiveAsync_WhenNoServices_ReturnsEmptyList()
    {
        // Arrange
        _repository.GetAllActiveAsync(Arg.Any<CancellationToken>()).Returns(new List<SalonService>());

        // Act
        var result = await _manager.GetAllActiveAsync(CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WhenServicesExist_ReturnsAllServicesIncludingInactive()
    {
        // Arrange
        var service1 = SalonService.Create("Active Service", 30, 100000m, "haircut");
        var service2 = SalonService.Create("Archived Service", 30, 100000m, "haircut");
        service2.Archive();

        _repository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<SalonService> { service1, service2 });

        // Act
        var result = await _manager.GetAllAsync(CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(s => s.Name == "Active Service" && s.IsActive);
        result.Should().Contain(s => s.Name == "Archived Service" && !s.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsMappedDto()
    {
        // Arrange
        var service = SalonService.Create("Haircut", "Desc", 45, 120000m, "haircut");
        _repository.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);

        // Act
        var result = await _manager.GetByIdAsync(service.Id, CancellationToken.None);

        // Assert
        result.Id.Should().Be(service.Id);
        result.Name.Should().Be("Haircut");
        result.Description.Should().Be("Desc");
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((SalonService?)null);

        // Act
        var act = () => _manager.GetByIdAsync(id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*{id}*");
    }

    [Fact]
    public async Task CreateAsync_WhenNameAlreadyExists_ThrowsValidationException()
    {
        // Arrange
        var request = new CreateSalonServiceRequest("Existing Service", "Desc", 30, 100000m, "haircut");
        _repository.ExistsByNameAsync(request.Name, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var act = () => _manager.CreateAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*already exists*");
        await _repository.DidNotReceive().AddAsync(Arg.Any<SalonService>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenValid_AddsAndSaves()
    {
        // Arrange
        var request = new CreateSalonServiceRequest("New Service", "Description", 45, 150000m, "haircut");
        _repository.ExistsByNameAsync(request.Name, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _manager.CreateAsync(request, CancellationToken.None);

        // Assert
        result.Name.Should().Be("New Service");
        result.DurationMinutes.Should().Be(45);
        result.Price.Should().Be(150000m);
        result.Category.Should().Be("haircut");
        result.IsActive.Should().BeTrue();

        await _repository.Received(1).AddAsync(Arg.Is<SalonService>(s => s.Name == "New Service"), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenServiceExists_UpdatesAndSaves()
    {
        // Arrange
        var service = SalonService.Create("Original", "Original Desc", 30, 100000m, "haircut");
        _repository.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        var request = new UpdateSalonServiceRequest("Updated", "Updated Desc", 60, 200000m, "color");

        // Act
        var result = await _manager.UpdateAsync(service.Id, request, CancellationToken.None);

        // Assert
        result.Name.Should().Be("Updated");
        result.DurationMinutes.Should().Be(60);
        result.Price.Should().Be(200000m);
        result.Category.Should().Be("color");

        _repository.Received(1).Update(service);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((SalonService?)null);
        var request = new UpdateSalonServiceRequest("Updated", "Updated Desc", 60, 200000m, "color");

        // Act
        var act = () => _manager.UpdateAsync(id, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ArchiveAsync_WhenExists_ArchivesAndSaves()
    {
        // Arrange
        var service = SalonService.Create("To Archive", 30, 100000m, "haircut");
        _repository.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);

        // Act
        await _manager.ArchiveAsync(service.Id, CancellationToken.None);

        // Assert
        service.IsActive.Should().BeFalse();
        _repository.Received(1).Update(service);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ArchiveAsync_WhenNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((SalonService?)null);

        // Act
        var act = () => _manager.ArchiveAsync(id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
