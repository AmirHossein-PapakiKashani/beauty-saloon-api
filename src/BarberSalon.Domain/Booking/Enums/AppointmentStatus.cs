namespace BarberSalon.Domain.Booking.Enums;

/// <summary>
/// The lifecycle status of an appointment.
/// </summary>
public enum AppointmentStatus
{
    /// <summary>Created; awaiting confirmation.</summary>
    Pending = 1,

    /// <summary>Confirmed by staff or salon admin.</summary>
    Confirmed = 2,

    /// <summary>Service was successfully delivered to the customer.</summary>
    Completed = 3,

    /// <summary>Cancelled by customer or admin.</summary>
    Cancelled = 4,

    /// <summary>Customer did not arrive for the appointment.</summary>
    NoShow = 5
}
