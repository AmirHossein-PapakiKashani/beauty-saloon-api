namespace BarberSalon.Application.Waitlist.DTOs;

public sealed record JoinWaitlistRequest(
    Guid CustomerId,
    string? CustomerName,
    string? CustomerPhone,
    Guid StaffId,
    string? StaffName,
    Guid ServiceId,
    string? ServiceName,
    string Date,
    string Time);
