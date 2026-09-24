namespace BarberSalon.Application.Reminders.DTOs;

public sealed record ReminderRuleDto(
    Guid Id,
    string Name,
    string Trigger,
    string Channel,
    string MessageTemplate,
    bool IsActive,
    DateTime CreatedAt
);

public sealed record CreateReminderRuleRequest(
    string Name,
    string Trigger,
    string Channel,
    string MessageTemplate
);
