using BarberSalon.Domain.Staff.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BarberSalon.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="StaffLeaveRequest"/> entity.
/// </summary>
public sealed class StaffLeaveRequestConfiguration : IEntityTypeConfiguration<StaffLeaveRequest>
{
    public void Configure(EntityTypeBuilder<StaffLeaveRequest> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.StaffMemberId)
            .IsRequired();

        builder.Property(r => r.LeaveType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(r => r.StartDate)
            .IsRequired();

        builder.Property(r => r.EndDate)
            .IsRequired();

        builder.Property(r => r.Reason)
            .HasMaxLength(1000);

        builder.Property(r => r.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(r => r.AdminComment)
            .HasMaxLength(1000);

        builder.HasIndex(r => new { r.StaffMemberId, r.Status })
            .HasDatabaseName("IX_StaffLeaveRequests_StaffMemberId_Status");

        builder.HasIndex(r => new { r.StartDate, r.EndDate })
            .HasDatabaseName("IX_StaffLeaveRequests_DateRange");

        builder.ToTable("StaffLeaveRequests");
    }
}
