namespace BarberSalon.Application.Staff.DTOs;

/// <summary>
/// Breakdown item for a completed appointment in the staff earnings summary.
/// </summary>
public sealed record StaffEarningItemDto(
    Guid AppointmentId,
    string CustomerName,
    string ServiceName,
    string Date,
    string Time,
    decimal ServicePrice,
    decimal CommissionRate,
    decimal CommissionEarned
);

/// <summary>
/// Performance and earnings report for a staff member over a time period.
/// </summary>
public sealed record StaffEarningsDto(
    Guid StaffId,
    string StaffName,
    int TotalAppointmentsCompleted,
    decimal TotalServiceRevenue,
    decimal TotalCommissionEarned,
    decimal DefaultCommissionRate,
    List<StaffEarningItemDto> Items
);
