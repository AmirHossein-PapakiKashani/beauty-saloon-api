using System.Globalization;
using BarberSalon.Application.Auth.Interfaces;
using BarberSalon.Application.Booking.DTOs;
using BarberSalon.Application.Booking.Interfaces;
using BarberSalon.Application.Common.Exceptions;
using BarberSalon.Application.Common.Interfaces;
using BarberSalon.Application.SalonServices.Interfaces;
using BarberSalon.Application.Staff.Interfaces;
using BarberSalon.Domain.Auth.Entities;
using BarberSalon.Domain.Booking.Entities;
using BarberSalon.Domain.Booking.Enums;
using BarberSalon.Domain.Booking.ValueObjects;
using BarberSalon.Domain.Common;
using BarberSalon.Domain.SalonServices.Entities;
using BarberSalon.Domain.Staff.Entities;

namespace BarberSalon.Application.Booking.Services;

/// <summary>
/// Application service managing booking use cases, appointment scheduling, and time slot availability.
/// </summary>
public sealed class BookingService
{
    private static readonly TimeOnly OperatingStart = new(9, 0);
    private static readonly TimeOnly OperatingEnd = new(18, 0);
    private const int SlotIntervalMinutes = 30;

    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IStaffRepository _staffRepository;
    private readonly ISalonServiceRepository _salonServiceRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    /// <summary>
    /// Initializes a new instance of <see cref="BookingService"/> with all required dependencies.
    /// </summary>
    /// <param name="appointmentRepository">Repository for appointment entities.</param>
    /// <param name="staffRepository">Repository for staff member entities.</param>
    /// <param name="salonServiceRepository">Repository for salon service entities.</param>
    /// <param name="userRepository">Repository for user/customer identities.</param>
    /// <param name="unitOfWork">Unit of work for persistence.</param>
    /// <param name="clock">Time provider abstraction.</param>
    public BookingService(
        IAppointmentRepository appointmentRepository,
        IStaffRepository staffRepository,
        ISalonServiceRepository salonServiceRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _appointmentRepository = appointmentRepository;
        _staffRepository = staffRepository;
        _salonServiceRepository = salonServiceRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    /// <summary>
    /// Backward-compatible constructor for availability queries.
    /// </summary>
    public BookingService(
        IAppointmentRepository appointmentRepository,
        IStaffRepository staffRepository,
        IClock clock)
        : this(
            appointmentRepository,
            staffRepository,
            null!,
            null!,
            null!,
            clock)
    {
    }

    /// <summary>
    /// Creates and persists a new appointment with domain validation and conflict checking.
    /// </summary>
    /// <param name="request">The appointment creation request payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A response containing the created appointment read model and unique booking code.</returns>
    /// <exception cref="ValidationException">Thrown when validation fails or slot is conflicting.</exception>
    /// <exception cref="NotFoundException">Thrown when customer, staff, or service does not exist.</exception>
    public async Task<CreateAppointmentResponse> CreateAppointmentAsync(
        CreateAppointmentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ValidationException("Request body cannot be null.");
        }

        if (request.CustomerId == Guid.Empty)
        {
            throw new ValidationException("CustomerId is required.");
        }

        if (request.StaffId == Guid.Empty)
        {
            throw new ValidationException("StaffId is required.");
        }

        if (request.ServiceId == Guid.Empty)
        {
            throw new ValidationException("ServiceId is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Date))
        {
            throw new ValidationException("Date cannot be empty. Expected yyyy-MM-dd format.");
        }

        if (!DateOnly.TryParseExact(request.Date.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            throw new ValidationException($"Invalid date format '{request.Date}'. Expected yyyy-MM-dd format.");
        }

        var today = DateOnly.FromDateTime(_clock.UtcNow);
        if (date < today)
        {
            throw new ValidationException("Appointment date cannot be in the past.");
        }

        if (string.IsNullOrWhiteSpace(request.Time))
        {
            throw new ValidationException("Time cannot be empty. Expected HH:mm format.");
        }

        if (!TimeOnly.TryParseExact(request.Time.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startTime))
        {
            throw new ValidationException($"Invalid time format '{request.Time}'. Expected HH:mm format.");
        }

        if (startTime < OperatingStart || startTime > OperatingEnd)
        {
            throw new ValidationException($"Appointment time must be between {OperatingStart:HH:mm} and {OperatingEnd:HH:mm}.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userRepository.GetByIdAsync(request.CustomerId, cancellationToken);
        if (user == null || !user.IsActive)
        {
            throw new NotFoundException(nameof(User), request.CustomerId);
        }

        var staff = await _staffRepository.GetByIdAsync(request.StaffId, cancellationToken);
        if (staff == null || !staff.IsActive)
        {
            throw new NotFoundException(nameof(StaffMember), request.StaffId);
        }

        var service = await _salonServiceRepository.GetByIdAsync(request.ServiceId, cancellationToken);
        if (service == null || !service.IsActive)
        {
            throw new NotFoundException(nameof(SalonService), request.ServiceId);
        }

        if (staff.ServiceIds.Count > 0 && !staff.ServiceIds.Contains(service.Id))
        {
            throw new ValidationException($"Staff member '{staff.FullName}' does not perform the selected service '{service.Name}'.");
        }

        var duration = service.DurationMinutes > 0 ? service.DurationMinutes : 30;
        var endTime = startTime.AddMinutes(duration);
        var timeSlot = TimeSlot.Create(date, startTime, endTime);

        var existingStaffAppointments = await _appointmentRepository.GetByStaffAndDateAsync(staff.Id, date, cancellationToken);
        var hasConflict = existingStaffAppointments.Any(a =>
            a.Status != AppointmentStatus.Cancelled &&
            a.Status != AppointmentStatus.NoShow &&
            a.TimeSlot.OverlapsWith(timeSlot));

        if (hasConflict)
        {
            throw new ValidationException("The selected time slot is already booked for this staff member.");
        }

        var appointment = Appointment.Create(
            user.Id,
            staff.Id,
            service.Id,
            timeSlot,
            service.Price,
            request.Notes);

        await _appointmentRepository.AddAsync(appointment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var bookingCode = GenerateBookingCode();
        var appointmentDto = MapToDto(appointment, user.FullName, staff.FullName, service.Name);

        return new CreateAppointmentResponse(appointmentDto, bookingCode);
    }

    /// <summary>
    /// Returns an appointment by its unique identifier.
    /// </summary>
    /// <param name="id">The appointment GUID identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The appointment read model DTO.</returns>
    /// <exception cref="NotFoundException">Thrown when appointment does not exist.</exception>
    public async Task<AppointmentDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Appointment), id);

        var user = await _userRepository.GetByIdAsync(appointment.CustomerId, cancellationToken);
        var staff = await _staffRepository.GetByIdAsync(appointment.StaffId, cancellationToken);
        var service = await _salonServiceRepository.GetByIdAsync(appointment.SalonServiceId, cancellationToken);

        return MapToDto(appointment, user?.FullName, staff?.FullName, service?.Name);
    }

    /// <summary>
    /// Updates the lifecycle status of an appointment.
    /// </summary>
    /// <param name="id">Appointment identifier.</param>
    /// <param name="status">Target status (confirmed, completed, cancelled, no_show).</param>
    /// <param name="reason">Optional cancellation reason.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated appointment read model DTO.</returns>
    /// <exception cref="NotFoundException">Thrown when appointment does not exist.</exception>
    /// <exception cref="ValidationException">Thrown when status is invalid or transition fails.</exception>
    public async Task<AppointmentDto> UpdateStatusAsync(
        Guid id,
        string status,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Appointment), id);

        var normalized = status?.Trim().ToLowerInvariant();
        try
        {
            switch (normalized)
            {
                case "confirmed":
                    appointment.Confirm();
                    break;
                case "completed":
                    appointment.Complete();
                    break;
                case "cancelled":
                    appointment.Cancel(reason);
                    break;
                case "no_show":
                case "noshow":
                    appointment.MarkNoShow();
                    break;
                default:
                    throw new ValidationException($"Invalid appointment status: '{status}'.");
            }
        }
        catch (DomainException ex)
        {
            throw new ValidationException(ex.Message);
        }

        _appointmentRepository.Update(appointment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var user = await _userRepository.GetByIdAsync(appointment.CustomerId, cancellationToken);
        var staff = await _staffRepository.GetByIdAsync(appointment.StaffId, cancellationToken);
        var service = await _salonServiceRepository.GetByIdAsync(appointment.SalonServiceId, cancellationToken);

        return MapToDto(appointment, user?.FullName, staff?.FullName, service?.Name);
    }

    /// <summary>
    /// Updates details of an existing appointment (reschedule time, change staff, change service, update price/notes).
    /// </summary>
    public async Task<AppointmentDto> UpdateAppointmentAsync(
        Guid id,
        UpdateAppointmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Appointment), id);

        if (!DateOnly.TryParseExact(request.Date.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            throw new ValidationException($"Invalid date format '{request.Date}'. Expected yyyy-MM-dd format.");
        }

        if (!TimeOnly.TryParseExact(request.StartTime.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startTime))
        {
            throw new ValidationException($"Invalid time format '{request.StartTime}'. Expected HH:mm format.");
        }

        if (startTime < OperatingStart || startTime > OperatingEnd)
        {
            throw new ValidationException($"Appointment time must be between {OperatingStart:HH:mm} and {OperatingEnd:HH:mm}.");
        }

        var staff = await _staffRepository.GetByIdAsync(request.StaffId, cancellationToken);
        if (staff == null || !staff.IsActive)
        {
            throw new NotFoundException(nameof(StaffMember), request.StaffId);
        }

        var service = await _salonServiceRepository.GetByIdAsync(request.SalonServiceId, cancellationToken);
        if (service == null || !service.IsActive)
        {
            throw new NotFoundException(nameof(SalonService), request.SalonServiceId);
        }

        var duration = service.DurationMinutes > 0 ? service.DurationMinutes : 30;
        var endTime = startTime.AddMinutes(duration);
        var timeSlot = TimeSlot.Create(date, startTime, endTime);

        var existingStaffAppointments = await _appointmentRepository.GetByStaffAndDateAsync(staff.Id, date, cancellationToken);
        var hasConflict = existingStaffAppointments.Any(a =>
            a.Id != appointment.Id &&
            a.Status != AppointmentStatus.Cancelled &&
            a.Status != AppointmentStatus.NoShow &&
            a.TimeSlot.OverlapsWith(timeSlot));

        if (hasConflict)
        {
            throw new ValidationException("The selected time slot is already booked for this staff member.");
        }

        var price = request.Price ?? (service.Price > 0 ? service.Price : appointment.Price);

        try
        {
            appointment.UpdateDetails(staff.Id, service.Id, timeSlot, price, request.Notes);
        }
        catch (DomainException ex)
        {
            throw new ValidationException(ex.Message);
        }

        _appointmentRepository.Update(appointment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var user = await _userRepository.GetByIdAsync(appointment.CustomerId, cancellationToken);

        return MapToDto(appointment, user?.FullName, staff?.FullName, service?.Name);
    }

    /// <summary>
    /// Returns all appointments for a given customer identifier, ordered by date descending.
    /// </summary>
    /// <param name="customerId">The customer GUID identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of appointment read model DTOs.</returns>
    /// <exception cref="ValidationException">Thrown when customer ID is empty.</exception>
    /// <exception cref="NotFoundException">Thrown when customer does not exist or is inactive.</exception>
    public async Task<List<AppointmentDto>> GetAppointmentsByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        if (customerId == Guid.Empty)
        {
            throw new ValidationException("CustomerId cannot be empty.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userRepository.GetByIdAsync(customerId, cancellationToken);
        if (user == null || !user.IsActive)
        {
            throw new NotFoundException(nameof(User), customerId);
        }

        var appointments = await _appointmentRepository.GetByCustomerIdAsync(customerId, cancellationToken);
        if (appointments.Count == 0)
        {
            return new List<AppointmentDto>();
        }

        var staffIds = appointments.Select(a => a.StaffId).Distinct().ToList();
        var serviceIds = appointments.Select(a => a.SalonServiceId).Distinct().ToList();

        var staffDict = new Dictionary<Guid, string>();
        foreach (var sId in staffIds)
        {
            var staff = await _staffRepository.GetByIdAsync(sId, cancellationToken);
            if (staff != null)
            {
                staffDict[sId] = staff.FullName;
            }
        }

        var serviceDict = new Dictionary<Guid, string>();
        foreach (var sId in serviceIds)
        {
            var service = await _salonServiceRepository.GetByIdAsync(sId, cancellationToken);
            if (service != null)
            {
                serviceDict[sId] = service.Name;
            }
        }

        return appointments.Select(a => MapToDto(
            a,
            user.FullName,
            staffDict.GetValueOrDefault(a.StaffId, "پرسنل"),
            serviceDict.GetValueOrDefault(a.SalonServiceId, "سرویس")
        )).ToList();
    }

    /// <summary>
    /// Returns all appointments across the entire salon, ordered by date descending and time descending.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of appointment read model DTOs.</returns>
    public async Task<List<AppointmentDto>> GetAllAppointmentsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var appointments = await _appointmentRepository.GetAllAsync(cancellationToken);
        if (appointments.Count == 0)
        {
            return new List<AppointmentDto>();
        }

        var customerIds = appointments.Select(a => a.CustomerId).Distinct().ToList();
        var staffIds = appointments.Select(a => a.StaffId).Distinct().ToList();
        var serviceIds = appointments.Select(a => a.SalonServiceId).Distinct().ToList();

        var customerDict = new Dictionary<Guid, string>();
        foreach (var cId in customerIds)
        {
            var user = await _userRepository.GetByIdAsync(cId, cancellationToken);
            if (user != null)
            {
                customerDict[cId] = user.FullName;
            }
        }

        var staffDict = new Dictionary<Guid, string>();
        foreach (var sId in staffIds)
        {
            var staff = await _staffRepository.GetByIdAsync(sId, cancellationToken);
            if (staff != null)
            {
                staffDict[sId] = staff.FullName;
            }
        }

        var serviceDict = new Dictionary<Guid, string>();
        foreach (var sId in serviceIds)
        {
            var service = await _salonServiceRepository.GetByIdAsync(sId, cancellationToken);
            if (service != null)
            {
                serviceDict[sId] = service.Name;
            }
        }

        return appointments.Select(a => MapToDto(
            a,
            customerDict.GetValueOrDefault(a.CustomerId, "مشتری"),
            staffDict.GetValueOrDefault(a.StaffId, "پرسنل"),
            serviceDict.GetValueOrDefault(a.SalonServiceId, "سرویس")
        )).ToList();
    }

    /// <summary>
    /// Returns all appointments for a given date in ISO format (yyyy-MM-dd), ordered by start time.
    /// </summary>
    /// <param name="dateString">The target date in yyyy-MM-dd format.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of appointment read model DTOs.</returns>
    /// <exception cref="ValidationException">Thrown when date is empty or invalid format.</exception>
    public async Task<List<AppointmentDto>> GetAppointmentsByDateAsync(
        string dateString,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dateString))
        {
            throw new ValidationException("Date cannot be empty. Expected yyyy-MM-dd format.");
        }

        if (!DateOnly.TryParseExact(dateString.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            throw new ValidationException($"Invalid date format '{dateString}'. Expected yyyy-MM-dd format.");
        }

        return await GetAppointmentsByDateAsync(date, cancellationToken);
    }

    /// <summary>
    /// Returns all appointments for a given <see cref="DateOnly"/> date, ordered by start time.
    /// </summary>
    /// <param name="date">The target date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of appointment read model DTOs.</returns>
    public async Task<List<AppointmentDto>> GetAppointmentsByDateAsync(
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var appointments = await _appointmentRepository.GetByDateAsync(date, cancellationToken);
        if (appointments.Count == 0)
        {
            return new List<AppointmentDto>();
        }

        var customerIds = appointments.Select(a => a.CustomerId).Distinct().ToList();
        var staffIds = appointments.Select(a => a.StaffId).Distinct().ToList();
        var serviceIds = appointments.Select(a => a.SalonServiceId).Distinct().ToList();

        var customerDict = new Dictionary<Guid, string>();
        foreach (var cId in customerIds)
        {
            var user = await _userRepository.GetByIdAsync(cId, cancellationToken);
            if (user != null)
            {
                customerDict[cId] = user.FullName;
            }
        }

        var staffDict = new Dictionary<Guid, string>();
        foreach (var sId in staffIds)
        {
            var staff = await _staffRepository.GetByIdAsync(sId, cancellationToken);
            if (staff != null)
            {
                staffDict[sId] = staff.FullName;
            }
        }

        var serviceDict = new Dictionary<Guid, string>();
        foreach (var sId in serviceIds)
        {
            var service = await _salonServiceRepository.GetByIdAsync(sId, cancellationToken);
            if (service != null)
            {
                serviceDict[sId] = service.Name;
            }
        }

        return appointments.Select(a => MapToDto(
            a,
            customerDict.GetValueOrDefault(a.CustomerId, "مشتری"),
            staffDict.GetValueOrDefault(a.StaffId, "پرسنل"),
            serviceDict.GetValueOrDefault(a.SalonServiceId, "سرویس")
        )).ToList();
    }

    /// <summary>
    /// Calculates available time slots for a given date and optional staff member.
    /// </summary>
    /// <param name="dateString">The requested booking date in ISO format (yyyy-MM-dd).</param>
    /// <param name="staffId">Optional staff member identifier. If omitted, checks salon-wide availability across all active staff.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of <see cref="TimeSlotDto"/> representing all daily slots and their availability.</returns>
    /// <exception cref="ValidationException">Thrown when date is empty, invalid format, or in the past.</exception>
    /// <exception cref="NotFoundException">Thrown when the specified staff member does not exist or is inactive.</exception>
    public async Task<List<TimeSlotDto>> GetAvailableTimeSlotsAsync(
        string dateString,
        Guid? staffId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dateString))
        {
            throw new ValidationException("Date cannot be empty. Expected yyyy-MM-dd format.");
        }

        if (!DateOnly.TryParseExact(dateString.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            throw new ValidationException($"Invalid date format '{dateString}'. Expected yyyy-MM-dd format.");
        }

        return await GetAvailableTimeSlotsAsync(date, staffId, cancellationToken);
    }

    /// <summary>
    /// Calculates available time slots for a given <see cref="DateOnly"/> date and optional staff member.
    /// </summary>
    /// <param name="date">The requested booking date.</param>
    /// <param name="staffId">Optional staff member identifier. If omitted, checks salon-wide availability.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of <see cref="TimeSlotDto"/> representing all daily slots and their availability.</returns>
    /// <exception cref="ValidationException">Thrown when the date is in the past.</exception>
    /// <exception cref="NotFoundException">Thrown when the specified staff member does not exist or is inactive.</exception>
    public async Task<List<TimeSlotDto>> GetAvailableTimeSlotsAsync(
        DateOnly date,
        Guid? staffId = null,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(_clock.UtcNow);
        if (date < today)
        {
            throw new ValidationException("Date cannot be in the past.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var dailySlotTimes = GenerateDailySlotTimes();

        if (staffId.HasValue)
        {
            var staffMember = await _staffRepository.GetByIdAsync(staffId.Value, cancellationToken);
            if (staffMember == null || !staffMember.IsActive)
            {
                throw new NotFoundException(nameof(StaffMember), staffId.Value);
            }

            var staffAppointments = await _appointmentRepository.GetByStaffAndDateAsync(staffId.Value, date, cancellationToken);

            var result = new List<TimeSlotDto>(dailySlotTimes.Count);
            foreach (var slotTime in dailySlotTimes)
            {
                var isBlocked = staffAppointments.Any(a => a.BlocksSlot(date, slotTime));
                result.Add(new TimeSlotDto(slotTime.ToString("HH:mm"), !isBlocked));
            }

            return result;
        }
        else
        {
            var activeStaff = await _staffRepository.GetAllActiveAsync(cancellationToken);

            if (activeStaff.Count == 0)
            {
                return dailySlotTimes
                    .Select(t => new TimeSlotDto(t.ToString("HH:mm"), false))
                    .ToList();
            }

            var activeStaffIds = activeStaff.Select(s => s.Id).ToHashSet();
            var salonAppointments = await _appointmentRepository.GetByDateAsync(date, cancellationToken);

            var result = new List<TimeSlotDto>(dailySlotTimes.Count);
            foreach (var slotTime in dailySlotTimes)
            {
                var busyStaffCount = salonAppointments
                    .Where(a => activeStaffIds.Contains(a.StaffId) && a.BlocksSlot(date, slotTime))
                    .Select(a => a.StaffId)
                    .Distinct()
                    .Count();

                var isAvailable = busyStaffCount < activeStaff.Count;
                result.Add(new TimeSlotDto(slotTime.ToString("HH:mm"), isAvailable));
            }

            return result;
        }
    }

    private static AppointmentDto MapToDto(
        Appointment a,
        string? customerName,
        string? staffName,
        string? serviceName)
    {
        return new AppointmentDto(
            a.Id,
            a.CustomerId,
            string.IsNullOrWhiteSpace(customerName) ? "مشتری" : customerName,
            a.StaffId,
            string.IsNullOrWhiteSpace(staffName) ? "پرسنل" : staffName,
            a.SalonServiceId,
            string.IsNullOrWhiteSpace(serviceName) ? "سرویس" : serviceName,
            a.TimeSlot.Date.ToString("yyyy-MM-dd"),
            a.TimeSlot.StartTime.ToString("HH:mm"),
            a.Status.ToString().ToLowerInvariant(),
            a.Price,
            a.Notes,
            a.CreatedAt,
            a.UpdatedAt
        );
    }

    private static string GenerateBookingCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = Random.Shared;
        var buffer = new char[7];
        buffer[0] = '#';
        for (int i = 1; i < 7; i++)
        {
            buffer[i] = chars[random.Next(chars.Length)];
        }
        return new string(buffer);
    }

    private static List<TimeOnly> GenerateDailySlotTimes()
    {
        var slots = new List<TimeOnly>();
        var current = OperatingStart;

        while (current <= OperatingEnd)
        {
            slots.Add(current);
            current = current.AddMinutes(SlotIntervalMinutes);
        }

        return slots;
    }
}
