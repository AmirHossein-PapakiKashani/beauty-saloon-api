using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Xunit;

namespace BarberSalon.IntegrationTests.Reminders;

public class DismissReminderIntegrationTests : IClassFixture<BarberSalonWebFactory>
{
    private readonly HttpClient _client;
    private readonly BarberSalonWebFactory _factory;

    public DismissReminderIntegrationTests(BarberSalonWebFactory factory)
    {
        _factory = factory;
        _client = factory.GetClient();
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private sealed record DismissReminderResponse(string Id, string Status);
    private sealed record ApiResponseEnvelope<T>(T Data, bool Success, string Message);

    [Fact]
    public async Task DismissReminder_WithValidReminderId_Returns200AndSetsStatusDismissed()
    {
        var reminderId = "rem_test_cust2_serv2";
        var response = await _client.PostAsync($"/api/v1/reminders/{reminderId}/dismiss", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<DismissReminderResponse>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Status.Should().Be("dismissed");
        envelope.Data.Id.Should().Be(reminderId);
    }
}
