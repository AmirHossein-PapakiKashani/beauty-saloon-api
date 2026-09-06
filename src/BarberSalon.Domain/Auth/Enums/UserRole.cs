namespace BarberSalon.Domain.Auth.Enums;

/// <summary>
/// The access level and role of a User in the salon system.
/// </summary>
public enum UserRole
{
    /// <summary>Customer who books appointments and manages their own profile.</summary>
    Customer = 1,

    /// <summary>Staff member who delivers salon services and views assigned appointments.</summary>
    Staff = 2,

    /// <summary>Administrator with full access to salon management, services, staff, and analytics.</summary>
    Admin = 3
}
