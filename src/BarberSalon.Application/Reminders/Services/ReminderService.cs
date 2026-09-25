using System.Collections.Concurrent;
using BarberSalon.Application.Booking.Interfaces;
using BarberSalon.Application.Common.Interfaces;
using BarberSalon.Application.Customers.Interfaces;
using BarberSalon.Application.Reminders.DTOs;
using BarberSalon.Application.Reminders.Interfaces;
using BarberSalon.Application.SalonServices.Interfaces;
using BarberSalon.Domain.Booking.Enums;
using BarberSalon.Domain.Reminders.Entities;

namespace BarberSalon.Application.Reminders.Services;

public sealed class ReminderService(
    IReminderRepository reminderRepository,
    IUnitOfWork unitOfWork,
    IAppointmentRepository appointmentRepository,
    ICustomerRepository customerRepository,
    ISalonServiceRepository salonServiceRepository)
{
    private static readonly ConcurrentDictionary<string, string> StatusOverrides = new();

    public async Task<List<DueReminderDto>> GetDueRemindersAsync(CancellationToken ct = default)
    {
        var appointments = await appointmentRepository.GetAllAsync(ct);
        var completedAppointments = appointments
            .Where(a => a.Status == AppointmentStatus.Completed)
            .ToList();

        var customers = (await customerRepository.GetAllAsync(ct)).ToDictionary(c => c.Id);
        var services = (await salonServiceRepository.GetAllAsync(ct)).ToDictionary(s => s.Id);

        var latestPerCustomerService = completedAppointments
            .GroupBy(a => (a.CustomerId, a.SalonServiceId))
            .Select(g => g.OrderByDescending(a => a.TimeSlot.Date).First())
            .ToList();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var dueList = new List<DueReminderDto>();

        foreach (var appt in latestPerCustomerService)
        {
            if (!customers.TryGetValue(appt.CustomerId, out var customer) ||
                !services.TryGetValue(appt.SalonServiceId, out var service))
            {
                continue;
            }

            var daysSinceVisit = today.DayNumber - appt.TimeSlot.Date.DayNumber;
            if (daysSinceVisit < 0) daysSinceVisit = 0;

            var category = (service.Category ?? "Other").Trim();
            var intervalDays = GetIntervalDays(category);
            var daysUntilDue = intervalDays - daysSinceVisit;

            var urgency = daysUntilDue < 0 ? "overdue" : daysUntilDue <= 3 ? "due" : "soon";
            var reminderId = $"rem_{customer.Id:N}_{service.Id:N}";

            var defaultStatus = (urgency == "overdue" || urgency == "due") ? "pending" : "scheduled";
            var finalStatus = StatusOverrides.TryGetValue(reminderId, out var overrideStatus) ? overrideStatus : defaultStatus;

            var message = $"وقت یادآوری برای {service.Name} فرا رسیده است. آخرین مراجعه {daysSinceVisit} روز قبل بود.";

            dueList.Add(new DueReminderDto(
                Id: reminderId,
                CustomerId: customer.Id,
                CustomerName: customer.FullName,
                CustomerPhone: customer.PhoneNumber,
                ServiceId: service.Id,
                ServiceName: service.Name,
                ServiceCategory: service.Category ?? "Other",
                LastVisitDate: appt.TimeSlot.Date.ToString("yyyy-MM-dd"),
                DaysSinceVisit: daysSinceVisit,
                IntervalDays: intervalDays,
                DaysUntilDue: daysUntilDue,
                Urgency: urgency,
                Status: finalStatus,
                Message: message,
                LastAppointmentId: appt.Id
            ));
        }

        return dueList.OrderByDescending(d => d.DaysSinceVisit).ToList();
    }

    private static int GetIntervalDays(string category) =>
        category.ToLowerInvariant() switch
        {
            var c when c.Contains("مو") || c.Contains("hair") => 28,
            var c when c.Contains("ریش") || c.Contains("beard") => 14,
            var c when c.Contains("رنگ") || c.Contains("color") => 35,
            var c when c.Contains("درمان") || c.Contains("treatment") || c.Contains("کراتین") => 21,
            var c when c.Contains("پوست") || c.Contains("skin") || c.Contains("facial") => 21,
            _ => 30
        };

    public Task<bool> SendReminderAsync(string id, CancellationToken ct = default)
    {
        StatusOverrides[id] = "sent";
        return Task.FromResult(true);
    }

    public Task<bool> DismissReminderAsync(string id, CancellationToken ct = default)
    {
        StatusOverrides[id] = "dismissed";
        return Task.FromResult(true);
    }

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
