using BarberSalon.Domain.Reviews.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BarberSalon.Infrastructure.Persistence.Configurations;

public sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Comment).HasMaxLength(1500);
        builder.Property(r => r.CustomerName).HasMaxLength(200);
        builder.Property(r => r.StaffName).HasMaxLength(200);
        builder.Property(r => r.ServiceName).HasMaxLength(200);
        builder.Property(r => r.Status).HasMaxLength(50).IsRequired();
        builder.Property(r => r.Rating).IsRequired();

        builder.HasIndex(r => r.CustomerId);
        builder.HasIndex(r => r.StaffId);
        builder.HasIndex(r => r.ServiceId);

        builder.ToTable("Reviews");
    }
}
