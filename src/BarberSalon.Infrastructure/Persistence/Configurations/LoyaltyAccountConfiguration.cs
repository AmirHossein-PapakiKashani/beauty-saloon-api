using BarberSalon.Domain.Loyalty.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BarberSalon.Infrastructure.Persistence.Configurations;

public sealed class LoyaltyAccountConfiguration : IEntityTypeConfiguration<LoyaltyAccount>
{
    public void Configure(EntityTypeBuilder<LoyaltyAccount> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Tier).HasMaxLength(50).IsRequired();
        builder.Property(l => l.ReferralCode).HasMaxLength(50).IsRequired();

        builder.HasIndex(l => l.CustomerId).IsUnique();
        builder.HasIndex(l => l.ReferralCode);

        builder.ToTable("LoyaltyAccounts");
    }
}
