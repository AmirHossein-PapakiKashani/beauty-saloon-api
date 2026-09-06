using BarberSalon.Application.Common.Exceptions;
using BarberSalon.Application.Common.Interfaces;
using BarberSalon.Application.Staff.DTOs;
using BarberSalon.Application.Staff.Interfaces;
using BarberSalon.Application.Staff.Services;
using BarberSalon.Domain.Staff.Entities;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace BarberSalon.Application.Tests.Staff;

public class StaffServiceTests
{
    private readonly IStaffRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly StaffService _service;

    public StaffServiceTests()
    {
        _repository = Substitute.For<IStaffRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _service = new StaffService(_repository, _unitOfWork);
    }

    [Fact]
    public async Task GetAllActiveAsync_WhenStaffExist_ReturnsMappedDtos()
    {
        // Arrange
        var staffList = new List<StaffMember>
        {
            StaffMember.Create("Ali Karimi", "karimi", "09121234567", "Bio 1", "Senior Barber", 10),
            StaffMember.Create("Sara Ahmadi", "ahmadi", "09129876543", "Bio 2", "Colorist", 8)
        };
        _repository.GetAllActiveAsync(Arg.Any<CancellationToken>()).Returns(staffList);

        // Act
        var result = await _service.GetAllActiveAsync(CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Ali Karimi");
        result[0].Slug.Should().Be("karimi");
        result[0].Phone.Should().Be("09121234567");
        result[0].Role.Should().Be("Senior Barber");
        result[0].IsActive.Should().BeTrue();
        result[1].Name.Should().Be("Sara Ahmadi");
    }

    [Fact]
    public async Task GetAllActiveAsync_WhenNoStaff_ReturnsEmptyList()
    {
        // Arrange
        _repository.GetAllActiveAsync(Arg.Any<CancellationToken>()).Returns(new List<StaffMember>());

        // Act
        var result = await _service.GetAllActiveAsync(CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WhenStaffExist_ReturnsAllStaffIncludingInactive()
    {
        // Arrange
        var staff1 = StaffMember.Create("Ali Karimi", "karimi", "09121234567", "Bio", "Barber", 5);
        var staff2 = StaffMember.Create("Reza Jafari", "jafari", "09125556677", "Bio", "Barber", 3);
        staff2.Archive();

        _repository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<StaffMember> { staff1, staff2 });

        // Act
        var result = await _service.GetAllAsync(CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(s => s.Slug == "karimi" && s.IsActive);
        result.Should().Contain(s => s.Slug == "jafari" && !s.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsMappedDto()
    {
        // Arrange
        var staff = StaffMember.Create("Ali Karimi", "karimi", "09121234567", "Bio", "Barber", 5);
        _repository.GetByIdAsync(staff.Id, Arg.Any<CancellationToken>()).Returns(staff);

        // Act
        var result = await _service.GetByIdAsync(staff.Id, CancellationToken.None);

        // Assert
        result.Id.Should().Be(staff.Id);
        result.Name.Should().Be("Ali Karimi");
        result.Slug.Should().Be("karimi");
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((StaffMember?)null);

        // Act
        var act = () => _service.GetByIdAsync(id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*{id}*");
    }

    [Fact]
    public async Task GetBySlugAsync_WhenExists_ReturnsMappedDto()
    {
        // Arrange
        var staff = StaffMember.Create("Ali Karimi", "karimi", "09121234567", "Bio", "Barber", 5);
        _repository.GetBySlugAsync("karimi", Arg.Any<CancellationToken>()).Returns(staff);

        // Act
        var result = await _service.GetBySlugAsync("karimi", CancellationToken.None);

        // Assert
        result.Id.Should().Be(staff.Id);
        result.Slug.Should().Be("karimi");
    }

    [Fact]
    public async Task GetBySlugAsync_WhenNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _repository.GetBySlugAsync("unknown", Arg.Any<CancellationToken>()).Returns((StaffMember?)null);

        // Act
        var act = () => _service.GetBySlugAsync("unknown", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*unknown*");
    }

    [Fact]
    public async Task GetByServiceIdAsync_WhenMatchesExist_ReturnsMappedDtos()
    {
        // Arrange
        var serviceId = Guid.NewGuid();
        var staff = StaffMember.Create("Ali Karimi", "karimi", "09121234567", "Bio", "Barber", 5, serviceIds: new List<Guid> { serviceId });
        _repository.GetByServiceIdAsync(serviceId, Arg.Any<CancellationToken>()).Returns(new List<StaffMember> { staff });

        // Act
        var result = await _service.GetByServiceIdAsync(serviceId, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Ali Karimi");
        result[0].Services.Should().Contain(serviceId);
    }

    [Fact]
    public async Task GetByServiceIdAsync_WhenNoMatches_ReturnsEmptyList()
    {
        // Arrange
        var serviceId = Guid.NewGuid();
        _repository.GetByServiceIdAsync(serviceId, Arg.Any<CancellationToken>()).Returns(new List<StaffMember>());

        // Act
        var result = await _service.GetByServiceIdAsync(serviceId, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_WhenSlugAlreadyExists_ThrowsValidationException()
    {
        // Arrange
        var request = new CreateStaffRequest("Ali Karimi", "karimi", "09121234567", "Bio", "Barber", 5);
        _repository.ExistsBySlugAsync(request.Slug, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var act = () => _service.CreateAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*already exists*");
        await _repository.DidNotReceive().AddAsync(Arg.Any<StaffMember>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenValid_AddsAndSaves()
    {
        // Arrange
        var request = new CreateStaffRequest("Ali Karimi", "karimi", "09121234567", "Bio", "Barber", 5);
        _repository.ExistsBySlugAsync(request.Slug, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _service.CreateAsync(request, CancellationToken.None);

        // Assert
        result.Name.Should().Be("Ali Karimi");
        result.Slug.Should().Be("karimi");
        result.Phone.Should().Be("09121234567");
        result.IsActive.Should().BeTrue();

        await _repository.Received(1).AddAsync(Arg.Is<StaffMember>(s => s.Slug == "karimi"), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ArchiveAsync_WhenExists_ArchivesAndSaves()
    {
        // Arrange
        var staff = StaffMember.Create("Ali Karimi", "karimi", "09121234567", "Bio", "Barber", 5);
        _repository.GetByIdAsync(staff.Id, Arg.Any<CancellationToken>()).Returns(staff);

        // Act
        await _service.ArchiveAsync(staff.Id, CancellationToken.None);

        // Assert
        staff.IsActive.Should().BeFalse();
        _repository.Received(1).Update(staff);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ArchiveAsync_WhenNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((StaffMember?)null);

        // Act
        var act = () => _service.ArchiveAsync(id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
