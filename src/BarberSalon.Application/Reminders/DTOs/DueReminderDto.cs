namespace BarberSalon.Application.Reminders.DTOs;

public sealed record DueReminderDto(
    string Id,
    Guid CustomerId,
    string CustomerName,
    string CustomerPhone,
    Guid ServiceId,
    string ServiceName,
    string ServiceCategory,
    string LastVisitDate,
    int DaysSinceVisit,
    int IntervalDays,
    int DaysUntilDue,
    string Urgency,
    string Status,
    string Message,
    Guid? LastAppointmentId
);
