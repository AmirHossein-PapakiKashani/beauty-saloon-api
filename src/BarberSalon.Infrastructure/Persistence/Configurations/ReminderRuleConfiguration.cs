using BarberSalon.Domain.Reminders.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BarberSalon.Infrastructure.Persistence.Configurations;

public sealed class ReminderRuleConfiguration : IEntityTypeConfiguration<ReminderRule>
{
    public void Configure(EntityTypeBuilder<ReminderRule> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Name).HasMaxLength(200).IsRequired();
        builder.Property(r => r.Trigger).HasMaxLength(100).IsRequired();
        builder.Property(r => r.Channel).HasMaxLength(50).IsRequired();
        builder.Property(r => r.MessageTemplate).HasMaxLength(1000).IsRequired();

        builder.ToTable("ReminderRules");
    }
}
