using BarberSalon.Domain.Staff.Entities;

namespace BarberSalon.Application.Staff.Interfaces;

/// <summary>
/// Data access contract for <see cref="StaffMember"/> entities.
/// </summary>
public interface IStaffRepository
{
    /// <summary>Returns a staff member by their unique identifier, or null if not found.</summary>
    Task<StaffMember?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns a staff member by their URL slug, or null if not found.</summary>
    Task<StaffMember?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>Returns all active staff members ordered by full name.</summary>
    Task<List<StaffMember>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns all staff members (active and archived). For Admin use.</summary>
    Task<List<StaffMember>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns all active staff members who perform the specified salon service.</summary>
    Task<List<StaffMember>> GetByServiceIdAsync(Guid serviceId, CancellationToken cancellationToken = default);

    /// <summary>Returns true if a staff member with the given slug already exists.</summary>
    Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>Adds a new staff member to the EF Core change tracker.</summary>
    Task AddAsync(StaffMember staffMember, CancellationToken cancellationToken = default);

    /// <summary>Marks a modified staff member as updated in the EF Core change tracker.</summary>
    void Update(StaffMember staffMember);
}
