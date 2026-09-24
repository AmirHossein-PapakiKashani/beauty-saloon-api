using BarberSalon.Domain.BeautyProfile.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BarberSalon.Infrastructure.Persistence.Configurations;

public sealed class BeautyProfileConfiguration : IEntityTypeConfiguration<CustomerBeautyProfile>
{
    public void Configure(EntityTypeBuilder<CustomerBeautyProfile> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.HairType).HasMaxLength(50);
        builder.Property(b => b.CurrentHairColor).HasMaxLength(100);
        builder.Property(b => b.Sensitivities).HasMaxLength(1000);
        builder.Property(b => b.Preferences).HasMaxLength(1000);
        builder.Property(b => b.Notes).HasMaxLength(2000);
        builder.Property(b => b.SkinType).HasMaxLength(50);
        builder.Property(b => b.Allergies).HasMaxLength(1000);

        builder.HasIndex(b => b.CustomerId).IsUnique();

        builder.ToTable("BeautyProfiles");
    }
}
