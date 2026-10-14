using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using BarberSalon.API.Common;
using BarberSalon.Application.Booking.DTOs;
using BarberSalon.Application.Staff.DTOs;
using BarberSalon.Domain.Auth.Entities;
using BarberSalon.Domain.Auth.Enums;
using BarberSalon.Domain.Booking.Entities;
using BarberSalon.Domain.Booking.Enums;
using BarberSalon.Domain.Booking.ValueObjects;
using BarberSalon.Domain.SalonServices.Entities;
using BarberSalon.Domain.Staff.Enums;
using BarberSalon.Infrastructure.Persistence;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BarberSalon.IntegrationTests.Staff;

public sealed class StaffPortalApiTests : IClassFixture<BarberSalonWebFactory>
{
    private readonly BarberSalonWebFactory _factory;
    private readonly HttpClient _client;

    public StaffPortalApiTests(BarberSalonWebFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static string CreateSessionToken(Guid userId, string phone, UserRole role)
    {
        var roleStr = role.ToString();
        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{phone}:{roleStr}"));
        return $"bs_{userId:N}_{payload}";
    }

    [Fact]
    public async Task GetStaffMe_WithoutToken_ReturnsUnauthorized()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/staff/me");
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetStaffMe_WithValidStaffSession_ReturnsStaffProfile()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var phone = $"0912{Random.Shared.Next(1000000, 9999999)}";
        var staffUserId = Guid.NewGuid();

        var createRequest = new CreateStaffRequest(
            Name: $"Staff {suffix}",
            Slug: $"staff-{suffix}",
            Phone: phone,
            Bio: "Bio text",
            Role: "Stylist",
            YearsExperience: 4,
            Specialties: new List<string> { "Hair" },
            Services: new List<Guid>(),
            UserId: staffUserId,
            AvatarUrl: "https://example.com/avatar.jpg",
            DefaultCommissionRate: 40m);

        var createRes = await _client.PostAsJsonAsync("/api/v1/staff", createRequest);
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);

        var token = CreateSessionToken(staffUserId, phone, UserRole.Staff);
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/staff/me");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(req);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<StaffDto>>();
        body.Should().NotBeNull();
        body!.Data.Name.Should().Be($"Staff {suffix}");
        body.Data.Role.Should().Be("Stylist");
        body.Data.DefaultCommissionRate.Should().Be(40m);
    }

    [Fact]
    public async Task UpdateStaffSelfProfile_UpdatesBioAndAvatar_PreservesRoleAndRates()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var phone = $"0912{Random.Shared.Next(1000000, 9999999)}";
        var staffUserId = Guid.NewGuid();

        var createRequest = new CreateStaffRequest(
            Name: $"Staff {suffix}",
            Slug: $"staff-{suffix}",
            Phone: phone,
            Bio: "Old Bio",
            Role: "Stylist",
            YearsExperience: 4,
            Specialties: new List<string> { "Hair" },
            Services: new List<Guid>(),
            UserId: staffUserId,
            DefaultCommissionRate: 45m);

        var createRes = await _client.PostAsJsonAsync("/api/v1/staff", createRequest);
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);

        var token = CreateSessionToken(staffUserId, phone, UserRole.Staff);
        var updateRequest = new UpdateStaffSelfProfileRequest("Updated Bio by Staff", "https://example.com/new-avatar.png");

        using var req = new HttpRequestMessage(HttpMethod.Patch, "/api/v1/staff/me/profile");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = JsonContent.Create(updateRequest);

        var response = await _client.SendAsync(req);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<StaffDto>>();
        body.Should().NotBeNull();
        body!.Data.Bio.Should().Be("Updated Bio by Staff");
        body.Data.AvatarUrl.Should().Be("https://example.com/new-avatar.png");
        body.Data.Role.Should().Be("Stylist");
        body.Data.DefaultCommissionRate.Should().Be(45m);
    }

    [Fact]
    public async Task GetStaffEarnings_ReturnsCommissionSummary()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var phone = $"0912{Random.Shared.Next(1000000, 9999999)}";
        var staffUserId = Guid.NewGuid();

        var createStaff = new CreateStaffRequest(
            Name: $"Staff {suffix}",
            Slug: $"staff-{suffix}",
            Phone: phone,
            Bio: "Bio",
            Role: "Stylist",
            YearsExperience: 5,
            Specialties: new List<string>(),
            Services: new List<Guid>(),
            UserId: staffUserId,
            DefaultCommissionRate: 50m);
        var createStaffRes = await _client.PostAsJsonAsync("/api/v1/staff", createStaff);
        var staff = (await createStaffRes.Content.ReadFromJsonAsync<ApiResponse<StaffDto>>())!.Data;

        var token = CreateSessionToken(staffUserId, phone, UserRole.Staff);
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/staff/me/earnings");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(req);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<StaffEarningsDto>>();
        body.Should().NotBeNull();
        body!.Data.StaffId.Should().Be(staff.Id);
        body.Data.DefaultCommissionRate.Should().Be(50m);
    }

    [Fact]
    public async Task RecordDelay_OnActiveAppointment_ReturnsOkAndRecordsMinutes()
    {
        // 1. Create staff
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var staffRes = await _client.PostAsJsonAsync("/api/v1/staff", new CreateStaffRequest(
            $"Staff {suffix}", $"staff-{suffix}", $"0912{Random.Shared.Next(1000000, 9999999)}", "Bio", "Barber", 3, new List<string>(), new List<Guid>()));
        var staff = (await staffRes.Content.ReadFromJsonAsync<ApiResponse<StaffDto>>())!.Data;

        // 2. Create customer and appointment via DbContext
        Guid apptId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var s = SalonService.Create($"Service {suffix}", 45, 800_000m, "Category");
            db.SalonServices.Add(s);

            var user = User.Create($"0912{Random.Shared.Next(1000000, 9999999)}", DateTime.UtcNow, UserRole.Customer, "Customer Test");
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
            var slot = TimeSlot.Create(date, new TimeOnly(11, 0), new TimeOnly(11, 45));
            var appt = Appointment.Create(user.Id, staff.Id, s.Id, slot, 800_000m);
            appt.Confirm();
            appt.StartService();
            db.Appointments.Add(appt);
            await db.SaveChangesAsync();
            apptId = appt.Id;
        }

        // 3. Call delay endpoint
        var delayRequest = new RecordDelayRequest(15);
        var delayRes = await _client.PostAsJsonAsync($"/api/v1/appointments/{apptId}/delay", delayRequest);
        delayRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var delayBody = await delayRes.Content.ReadFromJsonAsync<ApiResponse<AppointmentDto>>();
        delayBody.Should().NotBeNull();
        delayBody!.Data.DelayMinutes.Should().Be(15);
    }

    [Fact]
    public async Task StaffLeaves_SubmitAndAdminReview_WorkflowSucceeds()
    {
        // 1. Create staff member
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var phone = $"0912{Random.Shared.Next(1000000, 9999999)}";
        var staffUserId = Guid.NewGuid();

        var createStaff = new CreateStaffRequest(
            Name: $"Staff {suffix}",
            Slug: $"staff-{suffix}",
            Phone: phone,
            Bio: "Bio",
            Role: "Barber",
            YearsExperience: 3,
            Specialties: new List<string>(),
            Services: new List<Guid>(),
            UserId: staffUserId);
        var staffRes = await _client.PostAsJsonAsync("/api/v1/staff", createStaff);
        var staff = (await staffRes.Content.ReadFromJsonAsync<ApiResponse<StaffDto>>())!.Data;

        var staffToken = CreateSessionToken(staffUserId, phone, UserRole.Staff);
        var adminToken = "mock_admin_token";

        // 2. Submit leave request as staff
        var startDate = DateTime.UtcNow.Date.AddDays(2).AddHours(9);
        var endDate = startDate.AddHours(4);
        var submitRequest = new SubmitLeaveRequest("hourly", startDate, endDate, "دندانپزشکی");

        using var submitReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/staff/me/leaves");
        submitReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);
        submitReq.Content = JsonContent.Create(submitRequest);

        var submitRes = await _client.SendAsync(submitReq);
        submitRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var submitBody = await submitRes.Content.ReadFromJsonAsync<ApiResponse<StaffLeaveDto>>();
        submitBody.Should().NotBeNull();
        submitBody!.Data.Status.Should().Be("pending");
        submitBody.Data.Reason.Should().Be("دندانپزشکی");
        var leaveId = submitBody.Data.Id;

        // 3. Staff querying admin pending endpoint without admin role should get 403 Forbidden
        using var forbiddenReq = new HttpRequestMessage(HttpMethod.Get, "/api/v1/staff/admin/leaves/pending");
        forbiddenReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);
        var forbiddenRes = await _client.SendAsync(forbiddenReq);
        forbiddenRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // 4. Admin querying pending leaves should see the submitted leave
        using var pendingReq = new HttpRequestMessage(HttpMethod.Get, "/api/v1/staff/admin/leaves/pending");
        pendingReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var pendingRes = await _client.SendAsync(pendingReq);
        pendingRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var pendingBody = await pendingRes.Content.ReadFromJsonAsync<ApiResponse<List<StaffLeaveDto>>>();
        pendingBody.Should().NotBeNull();
        pendingBody!.Data.Should().Contain(l => l.Id == leaveId);

        // 5. Admin reviews and approves the leave
        var reviewRequest = new ReviewLeaveRequest(true, "تایید شد، مرخصی ثبت گردید.");
        using var reviewReq = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/staff/admin/leaves/{leaveId}/review");
        reviewReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        reviewReq.Content = JsonContent.Create(reviewRequest);

        var reviewRes = await _client.SendAsync(reviewReq);
        reviewRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var reviewBody = await reviewRes.Content.ReadFromJsonAsync<ApiResponse<StaffLeaveDto>>();
        reviewBody.Should().NotBeNull();
        reviewBody!.Data.Status.Should().Be("approved");
        reviewBody.Data.AdminComment.Should().Be("تایید شد، مرخصی ثبت گردید.");
    }

    [Fact]
    public async Task StaffAppointmentFilter_PreventsCrossStaffAccess()
    {
        var staff1UserId = Guid.NewGuid();
        var staff1Phone = $"0912{Random.Shared.Next(1000000, 9999999)}";
        var staff1Res = await _client.PostAsJsonAsync("/api/v1/staff", new CreateStaffRequest(
            Name: "Staff One",
            Slug: $"s1-{Guid.NewGuid():N}",
            Phone: staff1Phone,
            Bio: "Bio",
            Role: "Barber",
            YearsExperience: 3,
            Specialties: new List<string>(),
            Services: new List<Guid>(),
            UserId: staff1UserId));
        var staff1 = (await staff1Res.Content.ReadFromJsonAsync<ApiResponse<StaffDto>>())!.Data;

        var staff2UserId = Guid.NewGuid();
        var staff2Phone = $"0912{Random.Shared.Next(1000000, 9999999)}";
        var staff2Res = await _client.PostAsJsonAsync("/api/v1/staff", new CreateStaffRequest(
            Name: "Staff Two",
            Slug: $"s2-{Guid.NewGuid():N}",
            Phone: staff2Phone,
            Bio: "Bio",
            Role: "Barber",
            YearsExperience: 3,
            Specialties: new List<string>(),
            Services: new List<Guid>(),
            UserId: staff2UserId));
        var staff2 = (await staff2Res.Content.ReadFromJsonAsync<ApiResponse<StaffDto>>())!.Data;

        var staff1Token = CreateSessionToken(staff1UserId, staff1Phone, UserRole.Staff);

        // Staff 1 trying to query Staff 2's appointments should be forbidden
        using var crossReq = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/appointments?staffId={staff2.Id}");
        crossReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", staff1Token);

        var crossRes = await _client.SendAsync(crossReq);
        crossRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
