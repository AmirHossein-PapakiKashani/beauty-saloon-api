using BarberSalon.Domain.Auth.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BarberSalon.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="User"/> entity.
/// </summary>
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.PhoneNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(u => u.FullName)
            .HasMaxLength(100);

        builder.Property(u => u.Role)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(u => u.IsActive)
            .IsRequired();

        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.Property(u => u.UpdatedAt)
            .IsRequired();

        builder.HasIndex(u => u.PhoneNumber)
            .IsUnique()
            .HasDatabaseName("IX_Users_PhoneNumber");

        builder.ToTable("Users");
    }
}
