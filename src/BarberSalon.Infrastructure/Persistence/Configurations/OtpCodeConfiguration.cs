using BarberSalon.Domain.Auth.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BarberSalon.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="OtpCode"/> entity.
/// </summary>
public sealed class OtpCodeConfiguration : IEntityTypeConfiguration<OtpCode>
{
    public void Configure(EntityTypeBuilder<OtpCode> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.PhoneNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(o => o.Code)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(o => o.ExpiresAt)
            .IsRequired();

        builder.Property(o => o.IsUsed)
            .IsRequired();

        builder.Property(o => o.CreatedAt)
            .IsRequired();

        builder.Property(o => o.UpdatedAt)
            .IsRequired();

        builder.HasIndex(o => o.PhoneNumber)
            .HasDatabaseName("IX_OtpCodes_PhoneNumber");

        builder.HasIndex(o => new { o.PhoneNumber, o.IsUsed, o.ExpiresAt })
            .HasDatabaseName("IX_OtpCodes_Lookup");

        builder.ToTable("OtpCodes");
    }
}
