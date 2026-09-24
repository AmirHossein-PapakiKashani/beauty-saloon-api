using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BarberSalon.Application.Waitlist.DTOs;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Xunit;

namespace BarberSalon.IntegrationTests.Waitlist;

public class WaitlistIntegrationTests : IClassFixture<BarberSalonWebFactory>
{
    private const string Endpoint = "/api/v1/waitlist";

    private readonly HttpClient _client;
    private readonly BarberSalonWebFactory _factory;

    public WaitlistIntegrationTests(BarberSalonWebFactory factory)
    {
        _factory = factory;
        _client = factory.GetClient();
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task GetWaitlist_Returns200WithStandardEnvelope()
    {
        var response = await _client.GetAsync(Endpoint);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<List<WaitlistEntryDto>>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task Waitlist_FullLifecycle_JoinNotifyAndCancel()
    {
        var customerId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();

        var joinRequest = new JoinWaitlistRequest(
            CustomerId: customerId,
            CustomerName: "مشتری صف",
            CustomerPhone: "09121112233",
            StaffId: staffId,
            StaffName: "آرایشگر",
            ServiceId: serviceId,
            ServiceName: "خدمت کوتاهی",
            Date: "2026-09-25",
            Time: "14:00"
        );

        // 1. Join
        var postResponse = await _client.PostAsJsonAsync(Endpoint, joinRequest);
        postResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var joinEnvelope = await postResponse.Content.ReadFromJsonAsync<ApiResponseEnvelope<WaitlistEntryDto>>(JsonOptions);
        joinEnvelope.Should().NotBeNull();
        joinEnvelope!.Success.Should().BeTrue();
        var entry = joinEnvelope.Data;
        entry.CustomerName.Should().Be("مشتری صف");
        entry.Status.Should().Be("waiting");
        entry.QueuePosition.Should().BeGreaterThanOrEqualTo(1);

        // 2. Notify
        var notifyResponse = await _client.PostAsJsonAsync($"{Endpoint}/{entry.Id}/notify", new NotifyWaitlistRequest("نوبت شما آماده است"));
        notifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var notifyEnvelope = await notifyResponse.Content.ReadFromJsonAsync<ApiResponseEnvelope<WaitlistEntryDto>>(JsonOptions);
        notifyEnvelope.Should().NotBeNull();
        notifyEnvelope!.Data.Status.Should().Be("notified");
        notifyEnvelope.Data.NotifiedAt.Should().NotBeNull();

        // 3. Cancel / Delete
        var deleteResponse = await _client.DeleteAsync($"{Endpoint}/{entry.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed record ApiResponseEnvelope<T>(T Data, bool Success, string Message);
}
