using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BarberSalon.Application.Reminders.DTOs;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Xunit;

namespace BarberSalon.IntegrationTests.Reminders;

public class ToggleReminderRuleIntegrationTests : IClassFixture<BarberSalonWebFactory>
{
    private readonly HttpClient _client;
    private readonly BarberSalonWebFactory _factory;

    public ToggleReminderRuleIntegrationTests(BarberSalonWebFactory factory)
    {
        _factory = factory;
        _client = factory.GetClient();
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private sealed record ApiResponseEnvelope<T>(T Data, bool Success, string Message);

    [Fact]
    public async Task ToggleRule_WithExistingEnabledRule_Returns200AndTogglesRule()
    {
        // Arrange — create rule via API first
        var createRequest = new CreateReminderRuleRequest(
            "قانون تست تاگل",
            "48h_before",
            "sms",
            "یادآوری: نوبت شما پس‌فردا است."
        );

        var createResponse = await _client.PostAsJsonAsync("/api/v1/reminders/rules", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdEnvelope = await createResponse.Content.ReadFromJsonAsync<ApiResponseEnvelope<ReminderRuleDto>>(JsonOptions);
        createdEnvelope.Should().NotBeNull();
        createdEnvelope!.Data.IsActive.Should().BeTrue();
        var ruleId = createdEnvelope.Data.Id;

        // Act
        var toggleResponse = await _client.PatchAsync($"/api/v1/reminders/rules/{ruleId}/toggle", null);

        // Assert
        toggleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var toggledEnvelope = await toggleResponse.Content.ReadFromJsonAsync<ApiResponseEnvelope<ReminderRuleDto>>(JsonOptions);
        toggledEnvelope.Should().NotBeNull();
        toggledEnvelope!.Data.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task ToggleRule_WithNonExistentId_Returns404()
    {
        var response = await _client.PatchAsync($"/api/v1/reminders/rules/{Guid.NewGuid()}/toggle", null);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
