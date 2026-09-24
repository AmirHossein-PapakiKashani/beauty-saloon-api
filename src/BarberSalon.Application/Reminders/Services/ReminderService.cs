using BarberSalon.Application.Common.Interfaces;
using BarberSalon.Application.Reminders.DTOs;
using BarberSalon.Application.Reminders.Interfaces;
using BarberSalon.Domain.Reminders.Entities;

namespace BarberSalon.Application.Reminders.Services;

public sealed class ReminderService(
    IReminderRepository reminderRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<List<ReminderRuleDto>> GetRulesAsync(CancellationToken ct = default)
    {
        var rules = await reminderRepository.GetRulesAsync(ct);
        return rules.Select(Map).ToList();
    }

    public async Task<ReminderRuleDto?> GetRuleByIdAsync(Guid id, CancellationToken ct = default)
    {
        var rule = await reminderRepository.GetRuleByIdAsync(id, ct);
        return rule is null ? null : Map(rule);
    }

    public async Task<ReminderRuleDto> CreateRuleAsync(CreateReminderRuleRequest req, CancellationToken ct = default)
    {
        var rule = ReminderRule.Create(req.Name, req.Trigger, req.Channel, req.MessageTemplate);
        await reminderRepository.AddRuleAsync(rule, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Map(rule);
    }

    public async Task<ReminderRuleDto?> ToggleRuleAsync(Guid id, CancellationToken ct = default)
    {
        var rule = await reminderRepository.GetRuleByIdAsync(id, ct);
        if (rule is null) return null;

        rule.Toggle();
        await unitOfWork.SaveChangesAsync(ct);
        return Map(rule);
    }

    public async Task<bool> DeleteRuleAsync(Guid id, CancellationToken ct = default)
    {
        var rule = await reminderRepository.GetRuleByIdAsync(id, ct);
        if (rule is null) return false;

        reminderRepository.RemoveRule(rule);
        await unitOfWork.SaveChangesAsync(ct);
        return true;
    }

    private static ReminderRuleDto Map(ReminderRule r) =>
        new(r.Id, r.Name, r.Trigger, r.Channel, r.MessageTemplate, r.IsActive, r.CreatedAt);
}
