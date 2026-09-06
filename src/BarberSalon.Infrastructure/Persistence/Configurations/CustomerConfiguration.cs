using BarberSalon.Domain.Customers.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BarberSalon.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="Customer"/> entity.
/// </summary>
public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.FullName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.PhoneNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(c => c.Gender)
            .HasMaxLength(20);

        builder.Property(c => c.Notes)
            .HasMaxLength(1000);

        builder.Property(c => c.AppointmentsCount)
            .HasDefaultValue(0);

        builder.Property(c => c.IsActive)
            .HasDefaultValue(true);

        // Unique index on PhoneNumber
        builder.HasIndex(c => c.PhoneNumber)
            .IsUnique()
            .HasDatabaseName("IX_Customers_PhoneNumber");

        // Index on IsActive
        builder.HasIndex(c => c.IsActive)
            .HasDatabaseName("IX_Customers_IsActive");

        builder.ToTable("Customers");
    }
}
