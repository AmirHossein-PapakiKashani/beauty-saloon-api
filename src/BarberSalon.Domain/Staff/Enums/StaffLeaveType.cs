namespace BarberSalon.Domain.Staff.Enums;

/// <summary>
/// The type of leave or absence requested by a staff member.
/// </summary>
public enum StaffLeaveType
{
    /// <summary>Full-day leave covering the entire shift or calendar day.</summary>
    Daily = 1,

    /// <summary>Hourly absence covering a specific time window within a day.</summary>
    Hourly = 2
}
