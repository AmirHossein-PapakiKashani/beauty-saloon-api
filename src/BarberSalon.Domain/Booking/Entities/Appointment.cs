using BarberSalon.Domain.Booking.Enums;
using BarberSalon.Domain.Booking.ValueObjects;
using BarberSalon.Domain.Common;

namespace BarberSalon.Domain.Booking.Entities;

/// <summary>
/// A confirmed reservation created when a Customer books a SalonService with a StaffMember at a TimeSlot.
/// Central Aggregate Root of the booking process.
/// </summary>
public sealed class Appointment : BaseEntity
{
    /// <summary>Identifier of the customer who booked the appointment.</summary>
    public Guid CustomerId { get; private set; }

    /// <summary>Identifier of the staff member assigned to perform the service.</summary>
    public Guid StaffId { get; private set; }

    /// <summary>Identifier of the salon service to be delivered.</summary>
    public Guid SalonServiceId { get; private set; }

    /// <summary>The reserved date and time slot block.</summary>
    public TimeSlot TimeSlot { get; private set; } = default!;

    /// <summary>The current lifecycle status of the appointment.</summary>
    public AppointmentStatus Status { get; private set; }

    /// <summary>The agreed price of the service in local currency.</summary>
    public decimal Price { get; private set; }

    /// <summary>Optional notes or special requests for the appointment.</summary>
    public string Notes { get; private set; } = string.Empty;

    // Private constructor required by EF Core
    private Appointment() { }

    /// <summary>
    /// Creates a new <see cref="Appointment"/> instance in Pending status.
    /// </summary>
    /// <param name="customerId">Customer identifier.</param>
    /// <param name="staffId">Staff member identifier.</param>
    /// <param name="salonServiceId">Salon service identifier.</param>
    /// <param name="timeSlot">Reserved time slot.</param>
    /// <param name="price">Service price (must be non-negative).</param>
    /// <param name="notes">Optional notes.</param>
    /// <returns>A new <see cref="Appointment"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when any validation invariant fails.</exception>
    public static Appointment Create(
        Guid customerId,
        Guid staffId,
        Guid salonServiceId,
        TimeSlot timeSlot,
        decimal price,
        string? notes = null)
    {
        Validate(customerId, staffId, salonServiceId, timeSlot, price);

        return new Appointment
        {
            CustomerId = customerId,
            StaffId = staffId,
            SalonServiceId = salonServiceId,
            TimeSlot = timeSlot,
            Status = AppointmentStatus.Pending,
            Price = price,
            Notes = notes?.Trim() ?? string.Empty
        };
    }

    /// <summary>
    /// Confirms the appointment.
    /// </summary>
    /// <exception cref="DomainException">Thrown when appointment cannot be confirmed from its current status.</exception>
    public void Confirm()
    {
        if (Status != AppointmentStatus.Pending)
        {
            throw new DomainException($"Cannot confirm an appointment with status '{Status}'.");
        }

        Status = AppointmentStatus.Confirmed;
        Touch();
    }

    /// <summary>
    /// Marks the appointment as completed once the service has been delivered.
    /// </summary>
    /// <exception cref="DomainException">Thrown when appointment is not in Confirmed status.</exception>
    public void Complete()
    {
        if (Status != AppointmentStatus.Confirmed)
        {
            throw new DomainException($"Only confirmed appointments can be completed. Current status: '{Status}'.");
        }

        Status = AppointmentStatus.Completed;
        Touch();
    }

    /// <summary>
    /// Cancels the appointment.
    /// </summary>
    /// <param name="reason">Optional cancellation reason.</param>
    /// <exception cref="DomainException">Thrown when trying to cancel a completed, already cancelled, or no-show appointment.</exception>
    public void Cancel(string? reason = null)
    {
        if (Status == AppointmentStatus.Completed)
        {
            throw new DomainException("A completed appointment cannot be cancelled.");
        }

        if (Status == AppointmentStatus.Cancelled)
        {
            throw new DomainException("Appointment is already cancelled.");
        }

        if (Status == AppointmentStatus.NoShow)
        {
            throw new DomainException("Cannot cancel an appointment marked as no-show.");
        }

        Status = AppointmentStatus.Cancelled;
        if (!string.IsNullOrWhiteSpace(reason))
        {
            Notes = string.IsNullOrWhiteSpace(Notes)
                ? $"Cancellation reason: {reason.Trim()}"
                : $"{Notes} | Cancellation reason: {reason.Trim()}";
        }
        Touch();
    }

    /// <summary>
    /// Marks the appointment as no-show when the customer does not arrive.
    /// </summary>
    /// <exception cref="DomainException">Thrown when appointment is not in Confirmed status.</exception>
    public void MarkNoShow()
    {
        if (Status != AppointmentStatus.Confirmed)
        {
            throw new DomainException($"Only confirmed appointments can be marked as no-show. Current status: '{Status}'.");
        }

        Status = AppointmentStatus.NoShow;
        Touch();
    }

    /// <summary>
    /// Updates the appointment details (rescheduling slot, changing staff, service, price, notes).
    /// </summary>
    public void UpdateDetails(
        Guid staffId,
        Guid salonServiceId,
        TimeSlot timeSlot,
        decimal price,
        string? notes = null)
    {
        if (Status == AppointmentStatus.Completed)
        {
            throw new DomainException("Cannot update a completed appointment.");
        }

        if (Status == AppointmentStatus.Cancelled)
        {
            throw new DomainException("Cannot update a cancelled appointment.");
        }

        Validate(CustomerId, staffId, salonServiceId, timeSlot, price);

        StaffId = staffId;
        SalonServiceId = salonServiceId;
        TimeSlot = timeSlot;
        Price = price;
        if (notes != null)
        {
            Notes = notes.Trim();
        }
        Touch();
    }

    /// <summary>
    /// Checks if this appointment blocks a given slot time on a specific date.
    /// Active appointments (Pending, Confirmed, Completed) block time slots.
    /// Cancelled and NoShow appointments do not block time slots.
    /// </summary>
    public bool BlocksSlot(DateOnly date, TimeOnly slotTime)
    {
        if (TimeSlot.Date != date)
        {
            return false;
        }

        var isBlockingStatus = Status == AppointmentStatus.Pending
            || Status == AppointmentStatus.Confirmed
            || Status == AppointmentStatus.Completed;

        return isBlockingStatus && TimeSlot.Contains(slotTime);
    }

    private static void Validate(
        Guid customerId,
        Guid staffId,
        Guid salonServiceId,
        TimeSlot timeSlot,
        decimal price)
    {
        if (customerId == Guid.Empty)
            throw new DomainException("CustomerId cannot be empty.");

        if (staffId == Guid.Empty)
            throw new DomainException("StaffId cannot be empty.");

        if (salonServiceId == Guid.Empty)
            throw new DomainException("SalonServiceId cannot be empty.");

        if (timeSlot == null)
            throw new DomainException("TimeSlot cannot be null.");

        if (price < 0)
            throw new DomainException("Price cannot be negative.");
    }
}
