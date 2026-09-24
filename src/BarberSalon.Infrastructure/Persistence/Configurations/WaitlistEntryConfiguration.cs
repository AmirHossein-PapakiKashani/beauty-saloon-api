using BarberSalon.Domain.Waitlist.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BarberSalon.Infrastructure.Persistence.Configurations;

public sealed class WaitlistEntryConfiguration : IEntityTypeConfiguration<WaitlistEntry>
{
    public void Configure(EntityTypeBuilder<WaitlistEntry> builder)
    {
        builder.HasKey(w => w.Id);
        builder.Property(w => w.CustomerName).HasMaxLength(200);
        builder.Property(w => w.CustomerPhone).HasMaxLength(50);
        builder.Property(w => w.StaffName).HasMaxLength(200);
        builder.Property(w => w.ServiceName).HasMaxLength(200);
        builder.Property(w => w.Date).HasMaxLength(50);
        builder.Property(w => w.Time).HasMaxLength(50);
        builder.Property(w => w.Status).HasMaxLength(50).IsRequired();

        builder.HasIndex(w => w.CustomerId);
        builder.HasIndex(w => w.StaffId);
        builder.HasIndex(w => new { w.StaffId, w.Date, w.Status });

        builder.ToTable("WaitlistEntries");
    }
}
