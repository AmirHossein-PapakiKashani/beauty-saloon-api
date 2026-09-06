using BarberSalon.Domain.SalonServices.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BarberSalon.Infrastructure.Persistence.Configurations;

/// <summary>EF Core Fluent API configuration for the SalonService entity.</summary>
public sealed class SalonServiceConfiguration : IEntityTypeConfiguration<SalonService>
{
    public void Configure(EntityTypeBuilder<SalonService> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.Description)
            .HasMaxLength(500);

        builder.Property(s => s.Category)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(s => s.Price)
            .HasPrecision(18, 2);

        // Unique index — service names must be distinct
        builder.HasIndex(s => s.Name)
            .IsUnique()
            .HasDatabaseName("IX_SalonServices_Name");

        // Filtered index — most queries filter by IsActive
        builder.HasIndex(s => s.IsActive)
            .HasDatabaseName("IX_SalonServices_IsActive");

        builder.ToTable("SalonServices");
    }
}
