using BarberSalon.Domain.Common;

namespace BarberSalon.Domain.Reminders.Entities;

public sealed class ReminderRule : BaseEntity
{
    private ReminderRule() { }

    public string Name { get; private set; } = string.Empty;
    public string Trigger { get; private set; } = string.Empty;
    public string Channel { get; private set; } = "sms";
    public string MessageTemplate { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    public static ReminderRule Create(string name, string trigger, string channel, string messageTemplate)
    {
        return new ReminderRule
        {
            Name = name.Trim(),
            Trigger = trigger.Trim(),
            Channel = channel.Trim(),
            MessageTemplate = messageTemplate.Trim(),
            IsActive = true
        };
    }

    public void Toggle()
    {
        IsActive = !IsActive;
        Touch();
    }
}
