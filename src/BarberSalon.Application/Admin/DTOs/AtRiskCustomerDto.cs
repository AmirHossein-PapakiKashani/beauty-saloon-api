namespace BarberSalon.Application.Admin.DTOs;

public sealed record AtRiskCustomerDto(
    Guid CustomerId,
    string CustomerName,
    string Phone,
    string? LastAppointmentDate,
    int DaysSinceVisit,
    int AppointmentsCount,
    string RiskLevel,
    string SuggestedAction);
