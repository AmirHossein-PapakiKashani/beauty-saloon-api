using BarberSalon.Domain.Booking.Entities;

namespace BarberSalon.Application.Booking.Interfaces;

/// <summary>
/// Data access contract for <see cref="Appointment"/> entities.
/// </summary>
public interface IAppointmentRepository
{
    /// <summary>Returns an appointment by its unique identifier, or null if not found.</summary>
    Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns all appointments for a given staff member on a specific date.</summary>
    Task<List<Appointment>> GetByStaffAndDateAsync(Guid staffId, DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>Returns all appointments across the salon on a specific date.</summary>
    Task<List<Appointment>> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>Returns all appointments for a given customer ordered by date descending.</summary>
    Task<List<Appointment>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);

    /// <summary>Returns all appointments in the system. For Admin use.</summary>
    Task<List<Appointment>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns all appointments within an inclusive date range.</summary>
    Task<List<Appointment>> GetByDateRangeAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);

    /// <summary>Adds a new appointment to the EF Core change tracker.</summary>
    Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default);

    /// <summary>Marks a modified appointment as updated in the EF Core change tracker.</summary>
    void Update(Appointment appointment);
}

