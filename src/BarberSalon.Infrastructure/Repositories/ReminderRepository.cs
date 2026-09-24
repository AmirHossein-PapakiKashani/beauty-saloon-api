using BarberSalon.Application.Reminders.Interfaces;
using BarberSalon.Domain.Reminders.Entities;
using BarberSalon.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BarberSalon.Infrastructure.Repositories;

public sealed class ReminderRepository(AppDbContext context) : IReminderRepository
{
    public async Task<List<ReminderRule>> GetRulesAsync(CancellationToken ct = default)
    {
        return await context.ReminderRules.OrderBy(r => r.CreatedAt).ToListAsync(ct);
    }

    public async Task<ReminderRule?> GetRuleByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await context.ReminderRules.FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task AddRuleAsync(ReminderRule rule, CancellationToken ct = default)
    {
        await context.ReminderRules.AddAsync(rule, ct);
    }

    public void RemoveRule(ReminderRule rule)
    {
        context.ReminderRules.Remove(rule);
    }
}
