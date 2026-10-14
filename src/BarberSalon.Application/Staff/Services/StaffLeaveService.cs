using BarberSalon.Application.Common.Exceptions;
using BarberSalon.Application.Common.Interfaces;
using BarberSalon.Application.Staff.DTOs;
using BarberSalon.Application.Staff.Interfaces;
using BarberSalon.Domain.Common;
using BarberSalon.Domain.Staff.Entities;
using BarberSalon.Domain.Staff.Enums;

namespace BarberSalon.Application.Staff.Services;

/// <inheritdoc cref="IStaffLeaveService"/>
public sealed class StaffLeaveService : IStaffLeaveService
{
    private readonly IStaffLeaveRequestRepository _leaveRequestRepository;
    private readonly IStaffRepository _staffRepository;
    private readonly IUnitOfWork _unitOfWork;

    public StaffLeaveService(
        IStaffLeaveRequestRepository leaveRequestRepository,
        IStaffRepository staffRepository,
        IUnitOfWork unitOfWork)
    {
        _leaveRequestRepository = leaveRequestRepository;
        _staffRepository = staffRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<StaffLeaveDto> SubmitLeaveAsync(
        Guid staffMemberId,
        SubmitLeaveRequest request,
        CancellationToken cancellationToken = default)
    {
        var staff = await _staffRepository.GetByIdAsync(staffMemberId, cancellationToken)
            ?? throw new NotFoundException(nameof(StaffMember), staffMemberId);


        var leaveType = ParseLeaveType(request.LeaveType);

        var leave = StaffLeaveRequest.Create(
            staffMemberId: staffMemberId,
            leaveType: leaveType,
            startDate: request.StartDate,
            endDate: request.EndDate,
            reason: request.Reason
        );

        await _leaveRequestRepository.AddAsync(leave, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(leave, staff.FullName);
    }

    /// <inheritdoc />
    public async Task<List<StaffLeaveDto>> GetStaffLeavesAsync(
        Guid staffMemberId,
        CancellationToken cancellationToken = default)
    {
        var staff = await _staffRepository.GetByIdAsync(staffMemberId, cancellationToken);
        var staffName = staff?.FullName ?? "پرسنل";

        var leaves = await _leaveRequestRepository.GetByStaffIdAsync(staffMemberId, cancellationToken);
        return leaves.Select(l => MapToDto(l, staffName)).ToList();
    }

    /// <inheritdoc />
    public async Task<List<StaffLeaveDto>> GetPendingLeavesAsync(CancellationToken cancellationToken = default)
    {
        var pending = await _leaveRequestRepository.GetPendingAsync(cancellationToken);
        if (pending.Count == 0)
        {
            return new List<StaffLeaveDto>();
        }

        var allStaff = await _staffRepository.GetAllAsync(cancellationToken);
        var staffMap = allStaff.ToDictionary(s => s.Id, s => s.FullName);

        return pending.Select(l =>
        {
            var staffName = staffMap.TryGetValue(l.StaffMemberId, out var name) ? name : "پرسنل";
            return MapToDto(l, staffName);
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<StaffLeaveDto> ReviewLeaveAsync(
        Guid leaveId,
        ReviewLeaveRequest request,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        var leave = await _leaveRequestRepository.GetByIdAsync(leaveId, cancellationToken)
            ?? throw new NotFoundException(nameof(StaffLeaveRequest), leaveId);


        if (request.Approved)
        {
            leave.Approve(request.AdminComment, adminUserId);
        }
        else
        {
            leave.Reject(request.AdminComment, adminUserId);
        }

        _leaveRequestRepository.Update(leave);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var staff = await _staffRepository.GetByIdAsync(leave.StaffMemberId, cancellationToken);
        return MapToDto(leave, staff?.FullName ?? "پرسنل");
    }

    private static StaffLeaveType ParseLeaveType(string typeStr)
    {
        if (string.Equals(typeStr, "hourly", StringComparison.OrdinalIgnoreCase))
            return StaffLeaveType.Hourly;

        if (string.Equals(typeStr, "daily", StringComparison.OrdinalIgnoreCase))
            return StaffLeaveType.Daily;

        throw new DomainException($"Invalid leave type '{typeStr}'. Must be 'daily' or 'hourly'.");
    }

    private static StaffLeaveDto MapToDto(StaffLeaveRequest l, string staffName)
    {
        return new StaffLeaveDto(
            l.Id,
            l.StaffMemberId,
            staffName,
            l.LeaveType.ToString().ToLowerInvariant(),
            l.StartDate,
            l.EndDate,
            l.Reason,
            l.Status.ToString().ToLowerInvariant(),
            l.AdminComment,
            l.ReviewedAt,
            l.ReviewedBy,
            l.CreatedAt,
            l.UpdatedAt
        );
    }
}
