using BarberSalon.Application.Auth.Interfaces;
using BarberSalon.Application.Booking.Interfaces;
using BarberSalon.Application.Common.Exceptions;
using BarberSalon.Application.Common.Interfaces;
using BarberSalon.Application.PlatformAdmin.DTOs;
using BarberSalon.Application.Staff.Interfaces;
using BarberSalon.Domain.Auth.Entities;
using BarberSalon.Domain.Auth.Enums;
using BarberSalon.Domain.Common;
using BarberSalon.Domain.Salons.Entities;
using BarberSalon.Domain.Salons.Repositories;

namespace BarberSalon.Application.PlatformAdmin.Services;

public sealed class PlatformAdminService
{
    private readonly ISalonRepository _salonRepository;
    private readonly IUserRepository _userRepository;
    private readonly IStaffRepository _staffRepository;
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public PlatformAdminService(
        ISalonRepository salonRepository,
        IUserRepository userRepository,
        IStaffRepository staffRepository,
        IAppointmentRepository appointmentRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _salonRepository = salonRepository;
        _userRepository = userRepository;
        _staffRepository = staffRepository;
        _appointmentRepository = appointmentRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<PlatformMetricsDto> GetMetricsAsync(CancellationToken ct = default)
    {
        var totalSalons = await _salonRepository.CountAsync(null, ct);
        var activeSalons = await _salonRepository.CountAsync(true, ct);

        var allUsers = await _userRepository.GetAllAsync(ct);
        var now = _clock.UtcNow;
        var activeUsers24h = allUsers.Count(u => u.LastActiveAt.HasValue && u.LastActiveAt.Value >= now.AddHours(-24));

        var activeStaff = await _staffRepository.GetAllActiveAsync(ct);

        var today = DateOnly.FromDateTime(now);
        var appointments = await _appointmentRepository.GetAllAsync(ct);
        var todayAppointments = appointments.Count(a => a.TimeSlot.Date == today);

        return new PlatformMetricsDto(
            TotalUsers: allUsers.Count,
            ActiveUsersCount24h: activeUsers24h,
            TotalSalons: totalSalons,
            ActiveSalonsCount: activeSalons,
            ActiveStaffCount: activeStaff.Count,
            TodayAppointmentsCount: todayAppointments,
            SystemStatus: "Healthy"
        );
    }

    public async Task<IReadOnlyList<PlatformUserDto>> GetUsersAsync(
        string? search = null,
        UserRole? role = null,
        bool? isActive = null,
        CancellationToken ct = default)
    {
        var users = await _userRepository.GetAllAsync(ct);
        var query = users.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(u =>
                u.FullName.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                u.PhoneNumber.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        if (role.HasValue)
        {
            query = query.Where(u => u.Role == role.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        return query
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new PlatformUserDto(
                u.Id,
                u.PhoneNumber,
                u.FullName,
                u.Role,
                u.IsActive,
                u.CreatedAt,
                u.LastActiveAt))
            .ToList();
    }

    public async Task<PlatformUserDto> UpdateUserRoleAsync(Guid userId, UserRole newRole, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException(nameof(User), userId);

        user.UpdateRole(newRole);
        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(ct);

        return new PlatformUserDto(
            user.Id,
            user.PhoneNumber,
            user.FullName,
            user.Role,
            user.IsActive,
            user.CreatedAt,
            user.LastActiveAt);
    }

    public async Task<PlatformUserDto> ToggleUserStatusAsync(Guid userId, bool isActive, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException(nameof(User), userId);

        if (isActive && !user.IsActive)
        {
            user.Activate();
        }
        else if (!isActive && user.IsActive)
        {
            user.Archive();
        }

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(ct);

        return new PlatformUserDto(
            user.Id,
            user.PhoneNumber,
            user.FullName,
            user.Role,
            user.IsActive,
            user.CreatedAt,
            user.LastActiveAt);
    }

    public async Task<IReadOnlyList<PlatformSalonDto>> GetSalonsAsync(
        string? search = null,
        bool? isActive = null,
        CancellationToken ct = default)
    {
        var salons = await _salonRepository.GetAllAsync(includeArchived: true, ct);
        var query = salons.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(sl =>
                sl.Name.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                sl.Slug.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                sl.PhoneNumber.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        if (isActive.HasValue)
        {
            query = query.Where(sl => sl.IsActive == isActive.Value);
        }

        return query.Select(sl => new PlatformSalonDto(
            sl.Id,
            sl.Name,
            sl.Slug,
            sl.PhoneNumber,
            sl.Address,
            sl.Description,
            sl.OwnerUserId,
            sl.IsActive,
            sl.CreatedAt)).ToList();
    }

    public async Task<PlatformSalonDto> CreateSalonAsync(CreatePlatformSalonRequest request, CancellationToken ct = default)
    {
        if (request is null)
            throw new ValidationException("Request payload is required.");

        var exists = await _salonRepository.SlugExistsAsync(request.Slug, null, ct);
        if (exists)
            throw new DomainException($"A salon with slug '{request.Slug}' already exists.");

        var salon = Salon.Create(
            request.Name,
            request.Slug,
            request.PhoneNumber,
            request.Address,
            _clock.UtcNow,
            request.OwnerUserId,
            request.Description);

        await _salonRepository.AddAsync(salon, ct);

        return new PlatformSalonDto(
            salon.Id,
            salon.Name,
            salon.Slug,
            salon.PhoneNumber,
            salon.Address,
            salon.Description,
            salon.OwnerUserId,
            salon.IsActive,
            salon.CreatedAt);
    }

    public async Task<PlatformSalonDto> UpdateSalonAsync(Guid salonId, UpdatePlatformSalonRequest request, CancellationToken ct = default)
    {
        if (request is null)
            throw new ValidationException("Request payload is required.");

        var salon = await _salonRepository.GetByIdAsync(salonId, ct)
            ?? throw new NotFoundException(nameof(Salon), salonId);

        salon.UpdateDetails(request.Name, request.PhoneNumber, request.Address, request.Description);
        await _salonRepository.UpdateAsync(salon, ct);

        return new PlatformSalonDto(
            salon.Id,
            salon.Name,
            salon.Slug,
            salon.PhoneNumber,
            salon.Address,
            salon.Description,
            salon.OwnerUserId,
            salon.IsActive,
            salon.CreatedAt);
    }

    public async Task<PlatformSalonDto> ToggleSalonStatusAsync(Guid salonId, bool isActive, CancellationToken ct = default)
    {
        var salon = await _salonRepository.GetByIdAsync(salonId, ct)
            ?? throw new NotFoundException(nameof(Salon), salonId);

        if (isActive && !salon.IsActive)
            salon.Activate();
        else if (!isActive && salon.IsActive)
            salon.Archive();

        await _salonRepository.UpdateAsync(salon, ct);

        return new PlatformSalonDto(
            salon.Id,
            salon.Name,
            salon.Slug,
            salon.PhoneNumber,
            salon.Address,
            salon.Description,
            salon.OwnerUserId,
            salon.IsActive,
            salon.CreatedAt);
    }

    public async Task<IReadOnlyList<ActiveSessionUserDto>> GetActiveMonitoringAsync(int limit = 50, CancellationToken ct = default)
    {
        var users = await _userRepository.GetAllAsync(ct);
        var now = _clock.UtcNow;

        return users
            .Where(u => u.LastActiveAt.HasValue)
            .OrderByDescending(u => u.LastActiveAt)
            .Take(limit)
            .Select(u => new ActiveSessionUserDto(
                u.Id,
                u.FullName,
                u.PhoneNumber,
                u.Role,
                u.LastActiveAt,
                IsOnlineNow: u.LastActiveAt.HasValue && (now - u.LastActiveAt.Value).TotalMinutes <= 15))
            .ToList();
    }
}
