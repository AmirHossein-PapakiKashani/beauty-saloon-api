using BarberSalon.Domain.Auth.Enums;

namespace BarberSalon.Application.PlatformAdmin.DTOs;

public record PlatformMetricsDto(
    int TotalUsers,
    int ActiveUsersCount24h,
    int TotalSalons,
    int ActiveSalonsCount,
    int ActiveStaffCount,
    int TodayAppointmentsCount,
    string SystemStatus);

public record PlatformUserDto(
    Guid Id,
    string PhoneNumber,
    string FullName,
    UserRole Role,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastActiveAt);

public record UpdatePlatformUserRoleRequest(UserRole NewRole);

public record PlatformSalonDto(
    Guid Id,
    string Name,
    string Slug,
    string PhoneNumber,
    string Address,
    string? Description,
    Guid? OwnerUserId,
    bool IsActive,
    DateTime CreatedAt);

public record CreatePlatformSalonRequest(
    string Name,
    string Slug,
    string PhoneNumber,
    string Address,
    string? Description,
    Guid? OwnerUserId);

public record UpdatePlatformSalonRequest(
    string Name,
    string PhoneNumber,
    string Address,
    string? Description);

public record ActiveSessionUserDto(
    Guid Id,
    string FullName,
    string PhoneNumber,
    UserRole Role,
    DateTime? LastActiveAt,
    bool IsOnlineNow);
