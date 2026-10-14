using BarberSalon.Application.Staff.Interfaces;
using BarberSalon.Domain.Staff.Entities;
using BarberSalon.Domain.Staff.Enums;
using BarberSalon.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BarberSalon.Infrastructure.Repositories;

/// <inheritdoc cref="IStaffLeaveRequestRepository"/>
public sealed class StaffLeaveRequestRepository : IStaffLeaveRequestRepository
{
    private readonly AppDbContext _context;

    /// <summary>Initializes a new instance of <see cref="StaffLeaveRequestRepository"/>.</summary>
    public StaffLeaveRequestRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<StaffLeaveRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.StaffLeaveRequests
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<List<StaffLeaveRequest>> GetByStaffIdAsync(Guid staffMemberId, CancellationToken cancellationToken = default)
        => await _context.StaffLeaveRequests
            .Where(r => r.StaffMemberId == staffMemberId)
            .OrderByDescending(r => r.StartDate)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<List<StaffLeaveRequest>> GetPendingAsync(CancellationToken cancellationToken = default)
        => await _context.StaffLeaveRequests
            .Where(r => r.Status == StaffLeaveStatus.Pending)
            .OrderBy(r => r.StartDate)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<List<StaffLeaveRequest>> GetApprovedByStaffAndDateRangeAsync(
        Guid staffMemberId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
        => await _context.StaffLeaveRequests
            .Where(r => r.StaffMemberId == staffMemberId
                        && r.Status == StaffLeaveStatus.Approved
                        && r.StartDate < endDate
                        && r.EndDate > startDate)
            .OrderBy(r => r.StartDate)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(StaffLeaveRequest request, CancellationToken cancellationToken = default)
        => await _context.StaffLeaveRequests.AddAsync(request, cancellationToken);

    /// <inheritdoc />
    public void Update(StaffLeaveRequest request)
        => _context.StaffLeaveRequests.Update(request);
}
