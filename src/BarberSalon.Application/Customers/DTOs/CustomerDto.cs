namespace BarberSalon.Application.Customers.DTOs;

/// <summary>
/// Read model returned to clients representing a customer.
/// Maps directly to the frontend Customer type contract.
/// </summary>
public sealed record CustomerDto(
    Guid Id,
    string Name,
    string Phone,
    string? Gender,
    string Notes,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int AppointmentsCount,
    DateTime? LastAppointment,
    string Status
);
