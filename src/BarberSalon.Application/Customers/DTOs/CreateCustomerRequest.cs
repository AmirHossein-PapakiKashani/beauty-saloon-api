namespace BarberSalon.Application.Customers.DTOs;

/// <summary>
/// Input model for creating a new customer profile.
/// </summary>
public sealed record CreateCustomerRequest(
    string Name,
    string Phone,
    string? Gender = null,
    string? Notes = null
);
