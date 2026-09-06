using BarberSalon.Application.Booking.Interfaces;
using BarberSalon.Domain.Booking.Entities;
using BarberSalon.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BarberSalon.Infrastructure.Repositories;

/// <inheritdoc cref="IAppointmentRepository"/>
public sealed class AppointmentRepository : IAppointmentRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Initializes a new instance of <see cref="AppointmentRepository"/>.
    /// </summary>
    /// <param name="context">The EF Core database context.</param>
    public AppointmentRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Appointments
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<Appointment>> GetByStaffAndDateAsync(
        Guid staffId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        return await _context.Appointments
            .Where(a => a.StaffId == staffId && a.TimeSlot.Date == date)
            .OrderBy(a => a.TimeSlot.StartTime)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<Appointment>> GetByDateAsync(
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        return await _context.Appointments
            .Where(a => a.TimeSlot.Date == date)
            .OrderBy(a => a.TimeSlot.StartTime)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<Appointment>> GetByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Appointments
            .Where(a => a.CustomerId == customerId)
            .OrderByDescending(a => a.TimeSlot.Date)
            .ThenByDescending(a => a.TimeSlot.StartTime)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<Appointment>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Appointments
            .OrderByDescending(a => a.TimeSlot.Date)
            .ThenByDescending(a => a.TimeSlot.StartTime)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<Appointment>> GetByDateRangeAsync(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        return await _context.Appointments
            .Where(a => a.TimeSlot.Date >= startDate && a.TimeSlot.Date <= endDate)
            .OrderBy(a => a.TimeSlot.Date)
            .ThenBy(a => a.TimeSlot.StartTime)
            .ToListAsync(cancellationToken);
    }


    /// <inheritdoc />
    public async Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        await _context.Appointments.AddAsync(appointment, cancellationToken);
    }

    /// <inheritdoc />
    public void Update(Appointment appointment)
    {
        _context.Appointments.Update(appointment);
    }
}
