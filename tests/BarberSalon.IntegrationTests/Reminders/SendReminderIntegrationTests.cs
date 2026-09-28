using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Xunit;

namespace BarberSalon.IntegrationTests.Reminders;

public class SendReminderIntegrationTests : IClassFixture<BarberSalonWebFactory>
{
    private readonly HttpClient _client;
    private readonly BarberSalonWebFactory _factory;

    public SendReminderIntegrationTests(BarberSalonWebFactory factory)
    {
        _factory = factory;
        _client = factory.GetClient();
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private sealed record SendReminderResponse(string Id, string Status);
    private sealed record ApiResponseEnvelope<T>(T Data, bool Success, string Message);

    [Fact]
    public async Task SendReminder_WithValidReminderId_Returns200AndSetsStatusSent()
    {
        var reminderId = "rem_test_cust1_serv1";
        var response = await _client.PostAsync($"/api/v1/reminders/{reminderId}/send", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<SendReminderResponse>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Status.Should().Be("sent");
        envelope.Data.Id.Should().Be(reminderId);
    }
}
