using BarberSalon.Domain.Reminders.Entities;

namespace BarberSalon.Application.Reminders.Interfaces;

public interface IReminderRepository
{
    Task<List<ReminderRule>> GetRulesAsync(CancellationToken ct = default);
    Task<ReminderRule?> GetRuleByIdAsync(Guid id, CancellationToken ct = default);
    Task AddRuleAsync(ReminderRule rule, CancellationToken ct = default);
    void RemoveRule(ReminderRule rule);
}
