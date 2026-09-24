using BarberSalon.Domain.Portfolio.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BarberSalon.Infrastructure.Persistence.Configurations;

public sealed class PortfolioItemConfiguration : IEntityTypeConfiguration<PortfolioItem>
{
    public void Configure(EntityTypeBuilder<PortfolioItem> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Title).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Category).HasMaxLength(100).IsRequired();
        builder.Property(p => p.BeforeImageUrl).HasMaxLength(1000).IsRequired();
        builder.Property(p => p.AfterImageUrl).HasMaxLength(1000).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(2000);

        builder.HasIndex(p => p.Category);
        builder.HasIndex(p => p.StaffId);

        builder.ToTable("PortfolioItems");
    }
}
