using System.Text.Json;
using BarberSalon.Domain.Staff.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BarberSalon.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="StaffMember"/> entity.
/// </summary>
public sealed class StaffMemberConfiguration : IEntityTypeConfiguration<StaffMember>
{
    public void Configure(EntityTypeBuilder<StaffMember> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.FullName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.Slug)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.PhoneNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(s => s.Bio)
            .HasMaxLength(1000);

        builder.Property(s => s.Role)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.WorkingHoursJson)
            .HasMaxLength(2000);

        builder.Property(s => s.Specialties)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>()
            );

        builder.Property(s => s.ServiceIds)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>()
            );

        builder.HasIndex(s => s.Slug)
            .IsUnique()
            .HasDatabaseName("IX_StaffMembers_Slug");

        builder.HasIndex(s => s.IsActive)
            .HasDatabaseName("IX_StaffMembers_IsActive");

        builder.ToTable("StaffMembers");
    }
}
