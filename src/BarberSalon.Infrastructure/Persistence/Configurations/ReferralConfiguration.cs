using BarberSalon.Domain.Loyalty.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BarberSalon.Infrastructure.Persistence.Configurations;

public sealed class ReferralConfiguration : IEntityTypeConfiguration<Referral>
{
    public void Configure(EntityTypeBuilder<Referral> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.ReferrerName).HasMaxLength(200);
        builder.Property(r => r.ReferralCode).HasMaxLength(50).IsRequired();
        builder.Property(r => r.ReferredName).HasMaxLength(200);
        builder.Property(r => r.ReferredPhone).HasMaxLength(50);
        builder.Property(r => r.Status).HasMaxLength(50).IsRequired();

        builder.HasIndex(r => r.ReferrerCustomerId);
        builder.HasIndex(r => r.ReferralCode);
        builder.HasIndex(r => r.ReferredCustomerId);

        builder.ToTable("Referrals");
    }
}
