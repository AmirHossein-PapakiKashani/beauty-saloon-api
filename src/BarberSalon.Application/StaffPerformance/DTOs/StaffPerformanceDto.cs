namespace BarberSalon.Application.StaffPerformance.DTOs;

public sealed record StaffPerformanceDto(
    Guid StaffId,
    string StaffName,
    string Role,
    bool IsActive,
    int TotalAppointments,
    int CompletedAppointments,
    int ConfirmedAppointments,
    int CancelledAppointments,
    int NoShowAppointments,
    decimal TotalRevenue,
    double AverageRating,
    int ReviewCount,
    double CompletionRate,
    double CancellationRate,
    double NoShowRate,
    int PerformanceScore,
    int Rank
);
