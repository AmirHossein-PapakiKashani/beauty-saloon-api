using BarberSalon.API.Common;
using BarberSalon.Application.Reminders.DTOs;
using BarberSalon.Application.Reminders.Services;
using Microsoft.AspNetCore.Mvc;

namespace BarberSalon.API.Controllers;

[ApiController]
[Route("api/v1/reminders")]
public sealed class RemindersController(ReminderService reminderService) : ControllerBase
{
    [HttpGet("due")]
    [ProducesResponseType(typeof(ApiResponse<List<DueReminderDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDueReminders(CancellationToken ct)
    {
        var due = await reminderService.GetDueRemindersAsync(ct);
        return Ok(ApiResponse<List<DueReminderDto>>.CreateSuccess(due, "Due reminders retrieved successfully."));
    }

    [HttpPost("{id}/send")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SendReminder(string id, CancellationToken ct)
    {
        await reminderService.SendReminderAsync(id, ct);
        return Ok(ApiResponse<object>.CreateSuccess(new { id, status = "sent" }, "Reminder sent successfully."));
    }

    [HttpPost("{id}/dismiss")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DismissReminder(string id, CancellationToken ct)
    {
        await reminderService.DismissReminderAsync(id, ct);
        return Ok(ApiResponse<object>.CreateSuccess(new { id, status = "dismissed" }, "Reminder dismissed successfully."));
    }

    [HttpGet("rules")]
    [ProducesResponseType(typeof(ApiResponse<List<ReminderRuleDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRules(CancellationToken ct)
    {
        var rules = await reminderService.GetRulesAsync(ct);
        return Ok(ApiResponse<List<ReminderRuleDto>>.CreateSuccess(rules, "Reminder rules retrieved successfully."));
    }

    [HttpGet("rules/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ReminderRuleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRuleById(Guid id, CancellationToken ct)
    {
        var rule = await reminderService.GetRuleByIdAsync(id, ct);
        if (rule is null)
            return NotFound(new ApiResponse<object?>(null, false, $"Reminder rule '{id}' not found."));

        return Ok(ApiResponse<ReminderRuleDto>.CreateSuccess(rule, "Reminder rule retrieved successfully."));
    }

    [HttpPost("rules")]
    [ProducesResponseType(typeof(ApiResponse<ReminderRuleDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateRule([FromBody] CreateReminderRuleRequest req, CancellationToken ct)
    {
        var rule = await reminderService.CreateRuleAsync(req, ct);
        return Created($"/api/v1/reminders/rules/{rule.Id}",
            ApiResponse<ReminderRuleDto>.CreateSuccess(rule, "Reminder rule created successfully."));
    }

    [HttpPatch("rules/{id:guid}/toggle")]
    [ProducesResponseType(typeof(ApiResponse<ReminderRuleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ToggleRule(Guid id, CancellationToken ct)
    {
        var rule = await reminderService.ToggleRuleAsync(id, ct);
        if (rule is null)
            return NotFound(new ApiResponse<object?>(null, false, $"Reminder rule '{id}' not found."));

        return Ok(ApiResponse<ReminderRuleDto>.CreateSuccess(rule, "Reminder rule status toggled successfully."));
    }

    [HttpDelete("rules/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteRule(Guid id, CancellationToken ct)
    {
        var deleted = await reminderService.DeleteRuleAsync(id, ct);
        if (!deleted)
            return NotFound(new ApiResponse<object?>(null, false, $"Reminder rule '{id}' not found."));

        return Ok(ApiResponse<object?>.CreateSuccess(new { id }, "Reminder rule deleted successfully."));
    }
}
