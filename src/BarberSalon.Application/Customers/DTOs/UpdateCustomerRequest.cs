namespace BarberSalon.Application.Customers.DTOs;

/// <summary>
/// Input model for updating an existing customer profile.
/// </summary>
public sealed record UpdateCustomerRequest(
    string? Name = null,
    string? Phone = null,
    string? Gender = null,
    string? Notes = null,
    string? Status = null
);
