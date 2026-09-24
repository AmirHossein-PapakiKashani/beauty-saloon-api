using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BarberSalon.Application.Reminders.DTOs;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Xunit;

namespace BarberSalon.IntegrationTests.Reminders;

public class RemindersIntegrationTests : IClassFixture<BarberSalonWebFactory>
{
    private readonly HttpClient _client;
    private readonly BarberSalonWebFactory _factory;

    public RemindersIntegrationTests(BarberSalonWebFactory factory)
    {
        _factory = factory;
        _client = factory.GetClient();
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task GetRules_Returns200WithList()
    {
        var response = await _client.GetAsync("/api/v1/reminders/rules");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<List<ReminderRuleDto>>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAndDeleteReminderRule_ExecutesSuccessfully()
    {
        var createRequest = new CreateReminderRuleRequest(
            "Appointment 24h SMS Reminder",
            "24h_before",
            "sms",
            "یادآوری: نوبت شما فردا ساعت {time} می‌باشد."
        );

        var postResponse = await _client.PostAsJsonAsync("/api/v1/reminders/rules", createRequest);
        postResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var envelope = await postResponse.Content.ReadFromJsonAsync<ApiResponseEnvelope<ReminderRuleDto>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Name.Should().Be("Appointment 24h SMS Reminder");
        envelope.Data.IsActive.Should().BeTrue();

        var ruleId = envelope.Data.Id;

        var deleteResponse = await _client.DeleteAsync($"/api/v1/reminders/rules/{ruleId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed record ApiResponseEnvelope<T>(T Data, bool Success, string Message);
}
