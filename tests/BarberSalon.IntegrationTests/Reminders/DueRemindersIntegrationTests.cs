using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BarberSalon.Application.Reminders.DTOs;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Xunit;

namespace BarberSalon.IntegrationTests.Reminders;

public class DueRemindersIntegrationTests : IClassFixture<BarberSalonWebFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public DueRemindersIntegrationTests(BarberSalonWebFactory factory)
    {
        _client = factory.GetClient();
    }

    [Fact]
    public async Task GetDueReminders_Returns200WithList()
    {
        var response = await _client.GetAsync("/api/v1/reminders/due");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<List<DueReminderDto>>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task SendAndDismissReminder_ExecutesSuccessfully()
    {
        var testReminderId = "rem_test_123";

        // Send
        var sendRes = await _client.PostAsync($"/api/v1/reminders/{testReminderId}/send", null);
        sendRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Dismiss
        var dismissRes = await _client.PostAsync($"/api/v1/reminders/{testReminderId}/dismiss", null);
        dismissRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed record ApiResponseEnvelope<T>(T Data, bool Success, string Message);
}
