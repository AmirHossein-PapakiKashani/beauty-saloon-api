using BarberSalon.Application.Common.Exceptions;
using BarberSalon.Application.Common.Interfaces;
using BarberSalon.Application.Staff.DTOs;
using BarberSalon.Application.Staff.Interfaces;
using BarberSalon.Domain.Staff.Entities;

namespace BarberSalon.Application.Staff.Services;

/// <summary>
/// Application service orchestrating use cases for staff members.
/// </summary>
public sealed class StaffService
{
    private readonly IStaffRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Initializes a new instance of <see cref="StaffService"/>.</summary>
    public StaffService(IStaffRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>Returns all active staff members as DTOs.</summary>
    public async Task<List<StaffDto>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        var staff = await _repository.GetAllActiveAsync(cancellationToken);
        return staff.Select(MapToDto).ToList();
    }

    /// <summary>Returns all staff members (active and archived) as DTOs. For Admin use.</summary>
    public async Task<List<StaffDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var staff = await _repository.GetAllAsync(cancellationToken);
        return staff.Select(MapToDto).ToList();
    }

    /// <summary>Returns a single staff member by identifier.</summary>
    /// <exception cref="NotFoundException">Thrown when the staff member is not found.</exception>
    public async Task<StaffDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var member = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(StaffMember), id);

        return MapToDto(member);
    }

    /// <summary>Returns a single staff member by URL slug.</summary>
    /// <exception cref="NotFoundException">Thrown when the staff member is not found.</exception>
    public async Task<StaffDto> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var member = await _repository.GetBySlugAsync(slug, cancellationToken)
            ?? throw new NotFoundException(nameof(StaffMember), slug);

        return MapToDto(member);
    }

    /// <summary>Returns active staff members who provide the specified salon service.</summary>
    public async Task<List<StaffDto>> GetByServiceIdAsync(Guid serviceId, CancellationToken cancellationToken = default)
    {
        var staff = await _repository.GetByServiceIdAsync(serviceId, cancellationToken);
        return staff.Select(MapToDto).ToList();
    }

    /// <summary>Creates a new staff member.</summary>
    /// <exception cref="ValidationException">Thrown when slug already exists.</exception>
    public async Task<StaffDto> CreateAsync(CreateStaffRequest request, CancellationToken cancellationToken = default)
    {
        var slugExists = await _repository.ExistsBySlugAsync(request.Slug, cancellationToken);
        if (slugExists)
            throw new ValidationException($"A staff member with slug '{request.Slug}' already exists.");

        var member = StaffMember.Create(
            request.Name,
            request.Slug,
            request.Phone,
            request.Bio,
            request.Role,
            request.YearsExperience,
            request.Specialties,
            request.Services,
            request.UserId,
            request.WorkingHoursJson,
            request.ServicePrices);

        await _repository.AddAsync(member, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(member);
    }

    /// <summary>Updates an existing staff member.</summary>
    /// <exception cref="NotFoundException">Thrown when not found.</exception>
    /// <exception cref="ValidationException">Thrown when changed slug already exists.</exception>
    public async Task<StaffDto> UpdateAsync(Guid id, UpdateStaffRequest request, CancellationToken cancellationToken = default)
    {
        var member = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(StaffMember), id);

        if (!string.Equals(member.Slug, request.Slug.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            var slugExists = await _repository.ExistsBySlugAsync(request.Slug, cancellationToken);
            if (slugExists)
                throw new ValidationException($"A staff member with slug '{request.Slug}' already exists.");
        }

        member.Update(
            request.Name,
            request.Slug,
            request.Phone,
            request.Bio,
            request.Role,
            request.YearsExperience,
            request.Specialties,
            request.Services,
            request.UserId,
            request.WorkingHoursJson,
            request.ServicePrices);

        _repository.Update(member);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(member);
    }

    /// <summary>Archives a staff member (soft delete).</summary>
    /// <exception cref="NotFoundException">Thrown when not found.</exception>
    public async Task ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var member = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(StaffMember), id);

        member.Archive();
        _repository.Update(member);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Reactivates an archived staff member.</summary>
    /// <exception cref="NotFoundException">Thrown when not found.</exception>
    public async Task ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var member = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(StaffMember), id);

        member.Activate();
        _repository.Update(member);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static StaffDto MapToDto(StaffMember s) => new(
        s.Id,
        s.Slug,
        s.FullName,
        s.PhoneNumber,
        s.Bio,
        s.Role,
        s.IsActive,
        s.ServiceIds,
        0,
        s.YearsExperience,
        s.Specialties,
        s.WorkingHoursJson,
        s.CreatedAt,
        s.UpdatedAt,
        s.ServicePrices
    );
}
