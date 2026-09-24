using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BarberSalon.Application.Reviews.DTOs;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Xunit;

namespace BarberSalon.IntegrationTests.Reviews;

public class ReviewsIntegrationTests : IClassFixture<BarberSalonWebFactory>
{
    private const string Endpoint = "/api/v1/reviews";

    private readonly HttpClient _client;
    private readonly BarberSalonWebFactory _factory;

    public ReviewsIntegrationTests(BarberSalonWebFactory factory)
    {
        _factory = factory;
        _client = factory.GetClient();
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task GetReviews_WhenEndpointCalled_Returns200WithStandardEnvelope()
    {
        var response = await _client.GetAsync(Endpoint);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<List<ReviewDto>>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateReview_WithValidData_Returns201AndCanBeRetrieved()
    {
        var customerId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();

        var request = new CreateReviewRequest(
            CustomerId: customerId,
            CustomerName: "سارا احمدی",
            StaffId: staffId,
            StaffName: "مریم حسینی",
            ServiceId: serviceId,
            ServiceName: "کوتاهی مو",
            AppointmentId: null,
            Rating: 5,
            Comment: "بسیار عالی و حرفه‌ای"
        );

        var postResponse = await _client.PostAsJsonAsync(Endpoint, request);
        postResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createEnvelope = await postResponse.Content.ReadFromJsonAsync<ApiResponseEnvelope<ReviewDto>>(JsonOptions);
        createEnvelope.Should().NotBeNull();
        createEnvelope!.Success.Should().BeTrue();
        createEnvelope.Data.Should().NotBeNull();
        createEnvelope.Data.Comment.Should().Be("بسیار عالی و حرفه‌ای");
        createEnvelope.Data.Rating.Should().Be(5);
        createEnvelope.Data.Status.Should().Be("published");

        // Verify status update
        var updateStatusResponse = await _client.PutAsJsonAsync(
            $"{Endpoint}/{createEnvelope.Data.Id}/status",
            new UpdateReviewStatusRequest("hidden"));
        updateStatusResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify staff query
        var staffResponse = await _client.GetAsync($"{Endpoint}/staff/{staffId}");
        staffResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify delete
        var deleteResponse = await _client.DeleteAsync($"{Endpoint}/{createEnvelope.Data.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed record ApiResponseEnvelope<T>(T Data, bool Success, string Message);
}
