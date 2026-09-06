using BarberSalon.Domain.Booking.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BarberSalon.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="Appointment"/> entity.
/// </summary>
public sealed class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.CustomerId)
            .IsRequired();

        builder.Property(a => a.StaffId)
            .IsRequired();

        builder.Property(a => a.SalonServiceId)
            .IsRequired();

        builder.OwnsOne(a => a.TimeSlot, ts =>
        {
            ts.Property(p => p.Date)
                .IsRequired()
                .HasColumnName("Date");

            ts.Property(p => p.StartTime)
                .IsRequired()
                .HasColumnName("StartTime");

            ts.Property(p => p.EndTime)
                .IsRequired()
                .HasColumnName("EndTime");
        });

        builder.Property(a => a.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(a => a.Price)
            .HasPrecision(18, 2);

        builder.Property(a => a.Notes)
            .HasMaxLength(1000);

        builder.HasIndex(a => new { a.StaffId, a.Status })
            .HasDatabaseName("IX_Appointments_StaffId_Status");

        builder.HasIndex(a => a.CustomerId)
            .HasDatabaseName("IX_Appointments_CustomerId");

        builder.ToTable("Appointments");
    }
}
