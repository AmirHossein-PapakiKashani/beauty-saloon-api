using BarberSalon.Application.Auth.Interfaces;
using BarberSalon.Application.Booking.Interfaces;

using BarberSalon.Application.Common.Exceptions;
using BarberSalon.Application.Common.Interfaces;
using BarberSalon.Application.SalonServices.Interfaces;
using BarberSalon.Application.Staff.DTOs;
using BarberSalon.Application.Staff.Interfaces;
using BarberSalon.Domain.Booking.Enums;
using BarberSalon.Domain.Staff.Entities;

namespace BarberSalon.Application.Staff.Services;

/// <summary>
/// Application service orchestrating use cases for staff members.
/// </summary>
public sealed class StaffService
{
    private readonly IStaffRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAppointmentRepository? _appointmentRepository;
    private readonly ISalonServiceRepository? _salonServiceRepository;
    private readonly IUserRepository? _userRepository;

    /// <summary>Initializes a new instance of <see cref="StaffService"/>.</summary>
    public StaffService(
        IStaffRepository repository,
        IUnitOfWork unitOfWork,
        IAppointmentRepository? appointmentRepository = null,
        ISalonServiceRepository? salonServiceRepository = null,
        IUserRepository? userRepository = null)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _appointmentRepository = appointmentRepository;
        _salonServiceRepository = salonServiceRepository;
        _userRepository = userRepository;
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

        if (request.DefaultCommissionRate > 0 || request.ServiceCommissionRates?.Count > 0)
        {
            member.SetCommissionRates(request.DefaultCommissionRate, request.ServiceCommissionRates);
        }

        if (!string.IsNullOrWhiteSpace(request.AvatarUrl))
        {
            member.UpdateSelfProfile(request.Bio, request.AvatarUrl);
        }

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

        if (request.DefaultCommissionRate >= 0)
        {
            member.SetCommissionRates(request.DefaultCommissionRate, request.ServiceCommissionRates);
        }

        if (request.AvatarUrl != null)
        {
            member.UpdateSelfProfile(request.Bio, request.AvatarUrl);
        }

        _repository.Update(member);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(member);
    }

    /// <summary>Allows a staff member to self-update their bio and avatar.</summary>
    public async Task<StaffDto> UpdateSelfProfileAsync(Guid staffId, UpdateStaffSelfProfileRequest request, CancellationToken cancellationToken = default)
    {
        var member = await _repository.GetByIdAsync(staffId, cancellationToken)
            ?? throw new NotFoundException(nameof(StaffMember), staffId);

        member.UpdateSelfProfile(request.Bio, request.AvatarUrl);
        _repository.Update(member);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(member);
    }

    /// <summary>Computes completed appointment counts, revenues, and commissions for a staff member.</summary>
    public async Task<StaffEarningsDto> GetStaffEarningsAsync(
        Guid staffId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var member = await _repository.GetByIdAsync(staffId, cancellationToken)
            ?? throw new NotFoundException(nameof(StaffMember), staffId);

        if (_appointmentRepository == null)
        {
            return new StaffEarningsDto(
                member.Id,
                member.FullName,
                0,
                0m,
                0m,
                member.DefaultCommissionRate,
                new List<StaffEarningItemDto>()
            );
        }

        var start = startDate.HasValue ? DateOnly.FromDateTime(startDate.Value) : DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var end = endDate.HasValue ? DateOnly.FromDateTime(endDate.Value) : DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        var appointments = await _appointmentRepository.GetByStaffAndDateRangeAsync(staffId, start, end, cancellationToken);
        var completed = appointments.Where(a => a.Status == AppointmentStatus.Completed).ToList();

        var customerNames = new Dictionary<Guid, string>();
        if (_userRepository != null)
        {
            var customerIds = completed.Select(a => a.CustomerId).Distinct();
            foreach (var cid in customerIds)
            {
                var user = await _userRepository.GetByIdAsync(cid, cancellationToken);
                customerNames[cid] = user?.FullName ?? "مشتری";
            }
        }

        var serviceNames = new Dictionary<Guid, string>();
        if (_salonServiceRepository != null)
        {
            var serviceIds = completed.Select(a => a.SalonServiceId).Distinct();
            foreach (var sid in serviceIds)
            {
                var service = await _salonServiceRepository.GetByIdAsync(sid, cancellationToken);
                serviceNames[sid] = service?.Name ?? "سرویس";
            }
        }

        var items = completed.Select(a => new StaffEarningItemDto(
            a.Id,
            customerNames.TryGetValue(a.CustomerId, out var cName) ? cName : "مشتری",
            serviceNames.TryGetValue(a.SalonServiceId, out var sName) ? sName : "سرویس",
            a.TimeSlot.Date.ToString("yyyy-MM-dd"),
            a.TimeSlot.StartTime.ToString("HH:mm"),
            a.Price,
            a.CommissionRate,
            a.CommissionEarned
        )).ToList();

        var totalRevenue = completed.Sum(a => a.Price);
        var totalCommission = completed.Sum(a => a.CommissionEarned);

        return new StaffEarningsDto(
            member.Id,
            member.FullName,
            completed.Count,
            totalRevenue,
            totalCommission,
            member.DefaultCommissionRate,
            items
        );
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
        s.ServicePrices,
        s.AvatarUrl,
        s.DefaultCommissionRate,
        s.ServiceCommissionRates
    );
}
