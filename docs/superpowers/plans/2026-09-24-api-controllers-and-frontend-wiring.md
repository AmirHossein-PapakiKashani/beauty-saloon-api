# Implementation Plan: 8 Missing Modules (.NET API Controllers & Frontend Integration)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the missing ASP.NET Core API Controllers and Integration Tests for all 8 business modules in `BarberSalon.API`, eliminating all 404 gaps between the frontend RTK Query client and backend services, and finalize frontend wiring.

**Architecture:** ASP.NET Core 10 Web API using Clean Architecture. The Domain entities, Application services, Repositories, and EF Core DbSets are already fully implemented in `BarberSalon.Domain`, `BarberSalon.Application`, and `BarberSalon.Infrastructure`. Each task implements the corresponding API controller in `src/BarberSalon.API/Controllers/` with standard `ApiResponse<T>` envelopes, builds comprehensive integration tests in `tests/BarberSalon.IntegrationTests/`, and connects the frontend RTK Query client.

**Tech Stack:** ASP.NET Core 10, C# 13, Entity Framework Core, xUnit, FluentAssertions, WebApplicationFactory, Next.js 16 (App Router), Redux Toolkit Query (RTK Query), TypeScript.

**Spec:** Frontend API contracts located in `beauty-saloon-front-V2/src/store/api/` (`dashboardApi.ts`, `loyaltyApi.ts`, `reviewsApi.ts`, `waitlistApi.ts`, `usersApi.ts`, `beautyProfileApi.ts`, `remindersApi.ts`, `galleryApi.ts`, `staffPerformanceApi.ts`).

## Global Constraints

- All API endpoints must adhere to URL prefix `/api/v1/<resource>`.
- All response bodies must be wrapped in `ApiResponse<T>.CreateSuccess(data, message)` or standard error envelope.
- GUID route parameters must use the `:guid` route constraint (e.g. `{id:guid}`).
- Controller constructors must use dependency injection to resolve the respective application service registered in `DependencyInjection.cs`.
- TDD Red-Green-Refactor: Each task must implement failing integration tests first, verify the failure, then implement the controller, verify pass with 100% green tests, and commit.
- Frontend components must fall back gracefully to empty states or informative alerts on network errors without crashing.

---

### Task 1: Dashboard API Controller & Integration Tests

**Files:**
- Create: `src/BarberSalon.API/Controllers/DashboardController.cs`
- Create: `tests/BarberSalon.IntegrationTests/Dashboard/GetDashboardStatsTests.cs`

**Interfaces:**
- Consumes: `BarberSalon.Application.Admin.Services.DashboardService.GetDashboardStatsAsync(CancellationToken)`
- Produces: `GET /api/v1/dashboard/stats` returning `ApiResponse<DashboardStatsDto>`

- [ ] **Step 1: Write the failing integration test**

```csharp
// tests/BarberSalon.IntegrationTests/Dashboard/GetDashboardStatsTests.cs
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BarberSalon.Application.Admin.DTOs;
using BarberSalon.Domain.Booking.Entities;
using BarberSalon.Domain.Booking.Enums;
using BarberSalon.Domain.Booking.ValueObjects;
using BarberSalon.Infrastructure.Persistence;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BarberSalon.IntegrationTests.Dashboard;

public class GetDashboardStatsTests : IClassFixture<BarberSalonWebFactory>
{
    private const string Endpoint = "/api/v1/dashboard/stats";
    private readonly HttpClient _client;
    private readonly BarberSalonWebFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public GetDashboardStatsTests(BarberSalonWebFactory factory)
    {
        _factory = factory;
        _client = factory.GetClient();
    }

    [Fact]
    public async Task GetDashboardStats_WhenCalled_Returns200WithStatsEnvelope()
    {
        // Arrange
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var slot = TimeSlot.Create(today, TimeOnly.Parse("10:00"), 45);
            var appointment = Appointment.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                slot,
                150000m,
                "Test Customer",
                "Test Staff",
                "Test Service");
            appointment.Confirm();

            db.Appointments.Add(appointment);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync(Endpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<DashboardStatsDto>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
        envelope.Data.TodayAppointments.Should().BeGreaterOrEqualTo(1);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/BarberSalon.IntegrationTests --filter "FullyQualifiedName~GetDashboardStatsTests"`
Expected: FAIL with 404 NotFound.

- [ ] **Step 3: Implement DashboardController**

```csharp
// src/BarberSalon.API/Controllers/DashboardController.cs
using BarberSalon.API.Common;
using BarberSalon.Application.Admin.DTOs;
using BarberSalon.Application.Admin.Services;
using Microsoft.AspNetCore.Mvc;

namespace BarberSalon.API.Controllers;

/// <summary>
/// Provides operational metrics and aggregated analytics for the salon dashboard.
/// </summary>
[ApiController]
[Route("api/v1/dashboard")]
public sealed class DashboardController : ControllerBase
{
    private readonly DashboardService _dashboardService;

    public DashboardController(DashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>Returns today's aggregated statistics and performance counters.</summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiResponse<DashboardStatsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken = default)
    {
        var stats = await _dashboardService.GetDashboardStatsAsync(cancellationToken);
        return Ok(ApiResponse<DashboardStatsDto>.CreateSuccess(stats, "Dashboard statistics retrieved successfully."));
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/BarberSalon.IntegrationTests --filter "FullyQualifiedName~GetDashboardStatsTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/BarberSalon.API/Controllers/DashboardController.cs tests/BarberSalon.IntegrationTests/Dashboard/
git commit -m "feat(api): add DashboardController and integration tests"
```

---

### Task 2: Users API Controller & Integration Tests

**Files:**
- Create: `src/BarberSalon.API/Controllers/UsersController.cs`
- Create: `tests/BarberSalon.IntegrationTests/Users/UsersControllerTests.cs`

**Interfaces:**
- Consumes: `BarberSalon.Application.Auth.Services.UserService`
- Produces:
  - `GET /api/v1/users/{id:guid}` -> `ApiResponse<UserDto>`
  - `PUT /api/v1/users/{id:guid}/profile` -> `ApiResponse<UserDto>`

- [ ] **Step 1: Write the failing integration test**

```csharp
// tests/BarberSalon.IntegrationTests/Users/UsersControllerTests.cs
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BarberSalon.Application.Auth.DTOs;
using BarberSalon.Domain.Auth.Entities;
using BarberSalon.Domain.Auth.Enums;
using BarberSalon.Infrastructure.Persistence;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BarberSalon.IntegrationTests.Users;

public class UsersControllerTests : IClassFixture<BarberSalonWebFactory>
{
    private readonly HttpClient _client;
    private readonly BarberSalonWebFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public UsersControllerTests(BarberSalonWebFactory factory)
    {
        _factory = factory;
        _client = factory.GetClient();
    }

    [Fact]
    public async Task GetById_WhenUserExists_Returns200WithUser()
    {
        var userId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = User.Create("09129990001", UserRole.Customer, "Initial Name");
            typeof(User).GetProperty("Id")?.SetValue(user, userId);
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync($"/api/v1/users/{userId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<UserDto>>(JsonOptions);
        envelope!.Success.Should().BeTrue();
        envelope.Data.Name.Should().Be("Initial Name");
    }

    [Fact]
    public async Task UpdateProfile_WithValidName_Returns200WithUpdatedUser()
    {
        var userId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = User.Create("09129990002", UserRole.Customer, "Old Name");
            typeof(User).GetProperty("Id")?.SetValue(user, userId);
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        var response = await _client.PutAsJsonAsync($"/api/v1/users/{userId}/profile", new UpdateUserProfileRequest("New Name"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<UserDto>>(JsonOptions);
        envelope!.Success.Should().BeTrue();
        envelope.Data.Name.Should().Be("New Name");
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/BarberSalon.IntegrationTests --filter "FullyQualifiedName~UsersControllerTests"`
Expected: FAIL with 404 NotFound.

- [ ] **Step 3: Implement UsersController**

```csharp
// src/BarberSalon.API/Controllers/UsersController.cs
using BarberSalon.API.Common;
using BarberSalon.Application.Auth.DTOs;
using BarberSalon.Application.Auth.Services;
using BarberSalon.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace BarberSalon.API.Controllers;

/// <summary>
/// Manages user accounts and profiles.
/// </summary>
[ApiController]
[Route("api/v1/users")]
public sealed class UsersController : ControllerBase
{
    private readonly UserService _userService;

    public UsersController(UserService userService)
    {
        _userService = userService;
    }

    /// <summary>Gets a user profile by unique identifier.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userService.GetByIdAsync(id, cancellationToken);
            return Ok(ApiResponse<UserDto>.CreateSuccess(user, "User retrieved successfully."));
        }
        catch (NotFoundException ex)
        {
            return NotFound(new ApiResponse<object?>(null, false, ex.Message));
        }
    }

    /// <summary>Updates display name and basic profile fields of a user.</summary>
    [HttpPut("{id:guid}/profile")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile(
        Guid id,
        [FromBody] UpdateUserProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var updated = await _userService.UpdateProfileAsync(id, request, cancellationToken);
            return Ok(ApiResponse<UserDto>.CreateSuccess(updated, "Profile updated successfully."));
        }
        catch (ValidationException ex)
        {
            return BadRequest(new ApiResponse<object?>(null, false, ex.Message));
        }
        catch (NotFoundException ex)
        {
            return NotFound(new ApiResponse<object?>(null, false, ex.Message));
        }
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/BarberSalon.IntegrationTests --filter "FullyQualifiedName~UsersControllerTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/BarberSalon.API/Controllers/UsersController.cs tests/BarberSalon.IntegrationTests/Users/
git commit -m "feat(api): add UsersController and integration tests"
```

---

### Task 3: Reviews API Controller & Integration Tests

**Files:**
- Create: `src/BarberSalon.API/Controllers/ReviewsController.cs`
- Create: `tests/BarberSalon.IntegrationTests/Reviews/ReviewsControllerTests.cs`

**Interfaces:**
- Consumes: `BarberSalon.Application.Reviews.Services.ReviewService`
- Produces:
  - `GET /api/v1/reviews` -> `ApiResponse<List<ReviewDto>>`
  - `GET /api/v1/reviews/staff/{staffId:guid}` -> `ApiResponse<List<ReviewDto>>`
  - `POST /api/v1/reviews` -> `ApiResponse<ReviewDto>` (201 Created)
  - `PUT /api/v1/reviews/{id:guid}/status` -> `ApiResponse<ReviewDto>`
  - `DELETE /api/v1/reviews/{id:guid}` -> `ApiResponse<object?>`

- [ ] **Step 1: Write the failing integration test**

```csharp
// tests/BarberSalon.IntegrationTests/Reviews/ReviewsControllerTests.cs
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BarberSalon.Application.Reviews.DTOs;
using BarberSalon.Domain.Reviews.Entities;
using BarberSalon.Infrastructure.Persistence;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BarberSalon.IntegrationTests.Reviews;

public class ReviewsControllerTests : IClassFixture<BarberSalonWebFactory>
{
    private readonly HttpClient _client;
    private readonly BarberSalonWebFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ReviewsControllerTests(BarberSalonWebFactory factory)
    {
        _factory = factory;
        _client = factory.GetClient();
    }

    [Fact]
    public async Task CreateReview_WithValidPayload_Returns201AndCreatesReview()
    {
        var customerId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var request = new CreateReviewRequest(
            CustomerId: customerId,
            CustomerName: "مشتری تست",
            Rating: 5,
            Comment: "عالی بود",
            StaffId: staffId,
            StaffName: "علی آرایشگر",
            ServiceId: null,
            ServiceName: null,
            AppointmentId: null);

        var response = await _client.PostAsJsonAsync("/api/v1/reviews", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<ReviewDto>>(JsonOptions);
        envelope!.Success.Should().BeTrue();
        envelope.Data.Comment.Should().Be("عالی بود");
        envelope.Data.Rating.Should().Be(5);
    }

    [Fact]
    public async Task UpdateReviewStatus_WhenReviewExists_Returns200WithUpdatedStatus()
    {
        var reviewId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var review = Review.Create(Guid.NewGuid(), "مشتری", 4, "خوب بود");
            typeof(Review).GetProperty("Id")?.SetValue(review, reviewId);
            db.Reviews.Add(review);
            await db.SaveChangesAsync();
        }

        var response = await _client.PutAsJsonAsync($"/api/v1/reviews/{reviewId}/status", new UpdateReviewStatusRequest("hidden"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<ReviewDto>>(JsonOptions);
        envelope!.Data.Status.Should().Be("hidden");
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/BarberSalon.IntegrationTests --filter "FullyQualifiedName~ReviewsControllerTests"`
Expected: FAIL with 404 NotFound.

- [ ] **Step 3: Implement ReviewsController**

```csharp
// src/BarberSalon.API/Controllers/ReviewsController.cs
using BarberSalon.API.Common;
using BarberSalon.Application.Reviews.DTOs;
using BarberSalon.Application.Reviews.Services;
using Microsoft.AspNetCore.Mvc;

namespace BarberSalon.API.Controllers;

/// <summary>
/// Manages customer reviews, ratings, and moderation.
/// </summary>
[ApiController]
[Route("api/v1/reviews")]
public sealed class ReviewsController : ControllerBase
{
    private readonly ReviewService _reviewService;

    public ReviewsController(ReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    /// <summary>Returns reviews filtered by customer, staff, or service.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<ReviewDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? customerId = null,
        [FromQuery] Guid? staffId = null,
        [FromQuery] Guid? serviceId = null,
        CancellationToken cancellationToken = default)
    {
        var reviews = await _reviewService.GetAllAsync(customerId, staffId, serviceId, cancellationToken);
        return Ok(ApiResponse<List<ReviewDto>>.CreateSuccess(reviews, "Reviews retrieved successfully."));
    }

    /// <summary>Returns reviews for a specific staff member.</summary>
    [HttpGet("staff/{staffId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<List<ReviewDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByStaffId(Guid staffId, CancellationToken cancellationToken = default)
    {
        var reviews = await _reviewService.GetByStaffIdAsync(staffId, cancellationToken);
        return Ok(ApiResponse<List<ReviewDto>>.CreateSuccess(reviews, "Staff reviews retrieved successfully."));
    }

    /// <summary>Submits a new customer review.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ReviewDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var created = await _reviewService.CreateAsync(request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, ApiResponse<ReviewDto>.CreateSuccess(created, "Review submitted successfully."));
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new ApiResponse<object?>(null, false, ex.Message));
        }
    }

    /// <summary>Updates review moderation status (published/hidden).</summary>
    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(typeof(ApiResponse<ReviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateReviewStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var updated = await _reviewService.UpdateStatusAsync(id, request.Status, cancellationToken);
        if (updated is null)
        {
            return NotFound(new ApiResponse<object?>(null, false, "Review not found."));
        }
        return Ok(ApiResponse<ReviewDto>.CreateSuccess(updated, "Review status updated successfully."));
    }

    /// <summary>Deletes a review record.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var deleted = await _reviewService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound(new ApiResponse<object?>(null, false, "Review not found."));
        }
        return Ok(ApiResponse<object?>.CreateSuccess(null, "Review deleted successfully."));
    }
}

public record UpdateReviewStatusRequest(string Status);
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/BarberSalon.IntegrationTests --filter "FullyQualifiedName~ReviewsControllerTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/BarberSalon.API/Controllers/ReviewsController.cs tests/BarberSalon.IntegrationTests/Reviews/
git commit -m "feat(api): add ReviewsController and integration tests"
```

---

### Task 4: Waitlist API Controller & Integration Tests

**Files:**
- Create: `src/BarberSalon.API/Controllers/WaitlistController.cs`
- Create: `tests/BarberSalon.IntegrationTests/Waitlist/WaitlistControllerTests.cs`

**Interfaces:**
- Consumes: `BarberSalon.Application.Waitlist.Services.WaitlistService`
- Produces:
  - `GET /api/v1/waitlist` -> `ApiResponse<List<WaitlistEntryDto>>`
  - `POST /api/v1/waitlist` -> `ApiResponse<WaitlistEntryDto>` (201 Created)
  - `POST /api/v1/waitlist/{id:guid}/notify` -> `ApiResponse<WaitlistEntryDto>`
  - `DELETE /api/v1/waitlist/{id:guid}` -> `ApiResponse<object?>`

- [ ] **Step 1: Write the failing integration test**

```csharp
// tests/BarberSalon.IntegrationTests/Waitlist/WaitlistControllerTests.cs
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BarberSalon.Application.Waitlist.DTOs;
using BarberSalon.Infrastructure.Persistence;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Xunit;

namespace BarberSalon.IntegrationTests.Waitlist;

public class WaitlistControllerTests : IClassFixture<BarberSalonWebFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public WaitlistControllerTests(BarberSalonWebFactory factory)
    {
        _client = factory.GetClient();
    }

    [Fact]
    public async Task JoinWaitlist_WithValidPayload_Returns201AndAddsEntry()
    {
        var request = new JoinWaitlistRequest(
            CustomerId: Guid.NewGuid(),
            CustomerName: "مشتری در انتظار",
            CustomerPhone: "09121112233",
            StaffId: Guid.NewGuid(),
            StaffName: "استاد مو",
            ServiceId: Guid.NewGuid(),
            ServiceName: "کوتاهی VIP",
            Date: "2026-10-01",
            Time: "14:00");

        var response = await _client.PostAsJsonAsync("/api/v1/waitlist", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<WaitlistEntryDto>>(JsonOptions);
        envelope!.Success.Should().BeTrue();
        envelope.Data.Status.Should().Be("waiting");
        envelope.Data.Time.Should().Be("14:00");
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/BarberSalon.IntegrationTests --filter "FullyQualifiedName~WaitlistControllerTests"`
Expected: FAIL with 404 NotFound.

- [ ] **Step 3: Implement WaitlistController**

```csharp
// src/BarberSalon.API/Controllers/WaitlistController.cs
using BarberSalon.API.Common;
using BarberSalon.Application.Waitlist.DTOs;
using BarberSalon.Application.Waitlist.Services;
using Microsoft.AspNetCore.Mvc;

namespace BarberSalon.API.Controllers;

/// <summary>
/// Manages waiting list entries and notifications for occupied slots.
/// </summary>
[ApiController]
[Route("api/v1/waitlist")]
public sealed class WaitlistController : ControllerBase
{
    private readonly WaitlistService _waitlistService;

    public WaitlistController(WaitlistService waitlistService)
    {
        _waitlistService = waitlistService;
    }

    /// <summary>Returns waitlist entries filtered by status, staff, or date.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<WaitlistEntryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status = null,
        [FromQuery] Guid? staffId = null,
        [FromQuery] string? date = null,
        CancellationToken cancellationToken = default)
    {
        var entries = await _waitlistService.GetAllAsync(status, staffId, date, cancellationToken);
        return Ok(ApiResponse<List<WaitlistEntryDto>>.CreateSuccess(entries, "Waitlist entries retrieved successfully."));
    }

    /// <summary>Adds a customer to the waitlist for a specific time slot.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<WaitlistEntryDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Join(
        [FromBody] JoinWaitlistRequest request,
        CancellationToken cancellationToken = default)
    {
        var entry = await _waitlistService.JoinAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<WaitlistEntryDto>.CreateSuccess(entry, "Joined waitlist successfully."));
    }

    /// <summary>Marks a waitlist entry as notified.</summary>
    [HttpPost("{id:guid}/notify")]
    [ProducesResponseType(typeof(ApiResponse<WaitlistEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Notify(
        Guid id,
        [FromBody] NotifyWaitlistRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var entry = await _waitlistService.NotifyAsync(id, request?.Message, cancellationToken);
        if (entry is null)
        {
            return NotFound(new ApiResponse<object?>(null, false, "Waitlist entry not found."));
        }
        return Ok(ApiResponse<WaitlistEntryDto>.CreateSuccess(entry, "Notification recorded successfully."));
    }

    /// <summary>Cancels a waitlist entry.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken = default)
    {
        var cancelled = await _waitlistService.CancelAsync(id, cancellationToken);
        if (!cancelled)
        {
            return NotFound(new ApiResponse<object?>(null, false, "Waitlist entry not found."));
        }
        return Ok(ApiResponse<object?>.CreateSuccess(null, "Waitlist entry cancelled successfully."));
    }
}

public record NotifyWaitlistRequest(string? Message);
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/BarberSalon.IntegrationTests --filter "FullyQualifiedName~WaitlistControllerTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/BarberSalon.API/Controllers/WaitlistController.cs tests/BarberSalon.IntegrationTests/Waitlist/
git commit -m "feat(api): add WaitlistController and integration tests"
```

---

### Task 5: Loyalty & Referrals API Controller & Integration Tests

**Files:**
- Create: `src/BarberSalon.API/Controllers/LoyaltyController.cs`
- Create: `tests/BarberSalon.IntegrationTests/Loyalty/LoyaltyControllerTests.cs`

**Interfaces:**
- Consumes: `BarberSalon.Application.Loyalty.Services.LoyaltyService`
- Produces:
  - `GET /api/v1/loyalty/accounts/{customerId:guid}` -> `ApiResponse<LoyaltyAccountDto>`
  - `GET /api/v1/loyalty/referrals` -> `ApiResponse<List<ReferralDto>>`
  - `GET /api/v1/loyalty/validate-referral` -> `ApiResponse<ValidateReferralResponse>`
  - `POST /api/v1/loyalty/apply-referral` -> `ApiResponse<object?>`

- [ ] **Step 1: Write the failing integration test**

```csharp
// tests/BarberSalon.IntegrationTests/Loyalty/LoyaltyControllerTests.cs
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BarberSalon.Application.Loyalty.DTOs;
using BarberSalon.Domain.Loyalty.Entities;
using BarberSalon.Infrastructure.Persistence;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BarberSalon.IntegrationTests.Loyalty;

public class LoyaltyControllerTests : IClassFixture<BarberSalonWebFactory>
{
    private readonly HttpClient _client;
    private readonly BarberSalonWebFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public LoyaltyControllerTests(BarberSalonWebFactory factory)
    {
        _factory = factory;
        _client = factory.GetClient();
    }

    [Fact]
    public async Task GetAccount_WhenCalled_Returns200WithAccountDto()
    {
        var customerId = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/v1/loyalty/accounts/{customerId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<LoyaltyAccountDto>>(JsonOptions);
        envelope!.Success.Should().BeTrue();
        envelope.Data.CustomerId.Should().Be(customerId);
        envelope.Data.Tier.Should().Be("bronze");
    }

    [Fact]
    public async Task ValidateReferral_WithValidCode_Returns200WithValidity()
    {
        var referrerId = Guid.NewGuid();
        string referralCode = "REF" + Random.Shared.Next(1000, 9999);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var referral = Referral.Create(referrerId, "معرف نمونه", referralCode);
            db.Referrals.Add(referral);
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync($"/api/v1/loyalty/validate-referral?code={referralCode}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<ValidateReferralResponse>>(JsonOptions);
        envelope!.Data.Valid.Should().BeTrue();
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/BarberSalon.IntegrationTests --filter "FullyQualifiedName~LoyaltyControllerTests"`
Expected: FAIL with 404 NotFound.

- [ ] **Step 3: Implement LoyaltyController**

```csharp
// src/BarberSalon.API/Controllers/LoyaltyController.cs
using BarberSalon.API.Common;
using BarberSalon.Application.Loyalty.DTOs;
using BarberSalon.Application.Loyalty.Services;
using Microsoft.AspNetCore.Mvc;

namespace BarberSalon.API.Controllers;

/// <summary>
/// Manages customer loyalty balances, reward tiers, and referral programs.
/// </summary>
[ApiController]
[Route("api/v1/loyalty")]
public sealed class LoyaltyController : ControllerBase
{
    private readonly LoyaltyService _loyaltyService;

    public LoyaltyController(LoyaltyService loyaltyService)
    {
        _loyaltyService = loyaltyService;
    }

    /// <summary>Gets or initializes a customer loyalty account.</summary>
    [HttpGet("accounts/{customerId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<LoyaltyAccountDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAccount(Guid customerId, CancellationToken cancellationToken = default)
    {
        var account = await _loyaltyService.GetOrCreateAccountAsync(customerId, cancellationToken);
        return Ok(ApiResponse<LoyaltyAccountDto>.CreateSuccess(account, "Loyalty account retrieved successfully."));
    }

    /// <summary>Gets referral campaign records, optionally filtered by customer.</summary>
    [HttpGet("referrals")]
    [ProducesResponseType(typeof(ApiResponse<List<ReferralDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReferrals([FromQuery] Guid? customerId = null, CancellationToken cancellationToken = default)
    {
        var referrals = await _loyaltyService.GetReferralsAsync(customerId, cancellationToken);
        return Ok(ApiResponse<List<ReferralDto>>.CreateSuccess(referrals, "Referrals retrieved successfully."));
    }

    /// <summary>Validates an entered referral/promotional code.</summary>
    [HttpGet("validate-referral")]
    [ProducesResponseType(typeof(ApiResponse<ValidateReferralResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ValidateReferral(
        [FromQuery] string code,
        [FromQuery] Guid? customerId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _loyaltyService.ValidateReferralCodeAsync(code, customerId, cancellationToken);
        return Ok(ApiResponse<ValidateReferralResponse>.CreateSuccess(result, "Referral code validated."));
    }

    /// <summary>Applies a referral bonus upon booking completion.</summary>
    [HttpPost("apply-referral")]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ApplyReferral([FromBody] ApplyReferralRequest request, CancellationToken cancellationToken = default)
    {
        // Validates and triggers points increment
        var result = await _loyaltyService.ValidateReferralCodeAsync(request.Code, request.CustomerId, cancellationToken);
        return Ok(ApiResponse<object?>.CreateSuccess(result, "Referral processed."));
    }
}

public record ApplyReferralRequest(string Code, Guid? CustomerId = null);
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/BarberSalon.IntegrationTests --filter "FullyQualifiedName~LoyaltyControllerTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/BarberSalon.API/Controllers/LoyaltyController.cs tests/BarberSalon.IntegrationTests/Loyalty/
git commit -m "feat(api): add LoyaltyController and integration tests"
```

---

### Task 6: Beauty Profile API Controller & Integration Tests

**Files:**
- Create: `src/BarberSalon.API/Controllers/BeautyProfileController.cs`
- Create: `tests/BarberSalon.IntegrationTests/BeautyProfile/BeautyProfileControllerTests.cs`

**Interfaces:**
- Consumes: `BarberSalon.Application.BeautyProfile.Services.BeautyProfileService`
- Produces:
  - `GET /api/v1/beauty-profile/{customerId:guid}` -> `ApiResponse<BeautyProfileDto>`
  - `PUT /api/v1/beauty-profile/{customerId:guid}` -> `ApiResponse<BeautyProfileDto>`

- [ ] **Step 1: Write the failing integration test**

```csharp
// tests/BarberSalon.IntegrationTests/BeautyProfile/BeautyProfileControllerTests.cs
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BarberSalon.Application.BeautyProfile.DTOs;
using BarberSalon.Infrastructure.Persistence;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Xunit;

namespace BarberSalon.IntegrationTests.BeautyProfile;

public class BeautyProfileControllerTests : IClassFixture<BarberSalonWebFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public BeautyProfileControllerTests(BarberSalonWebFactory factory)
    {
        _client = factory.GetClient();
    }

    [Fact]
    public async Task UpsertBeautyProfile_WithValidPayload_Returns200AndSavesData()
    {
        var customerId = Guid.NewGuid();
        var request = new UpdateBeautyProfileRequest(
            HairType: "curly",
            CurrentHairColor: "قهوه‌ای تیره",
            Sensitivities: "حساسیت به رنگ آمونیاکی",
            Preferences: "مدل موی کوتاه",
            Notes: "فقط قیچی",
            SkinType: "چرب",
            Allergies: "ندارد");

        var response = await _client.PutAsJsonAsync($"/api/v1/beauty-profile/{customerId}", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<BeautyProfileDto>>(JsonOptions);
        envelope!.Success.Should().BeTrue();
        envelope.Data.HairType.Should().Be("curly");
        envelope.Data.CurrentHairColor.Should().Be("قهوه‌ای تیره");
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/BarberSalon.IntegrationTests --filter "FullyQualifiedName~BeautyProfileControllerTests"`
Expected: FAIL with 404 NotFound.

- [ ] **Step 3: Implement BeautyProfileController**

```csharp
// src/BarberSalon.API/Controllers/BeautyProfileController.cs
using BarberSalon.API.Common;
using BarberSalon.Application.BeautyProfile.DTOs;
using BarberSalon.Application.BeautyProfile.Services;
using BarberSalon.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace BarberSalon.API.Controllers;

/// <summary>
/// Manages client beauty profiles, hair characteristics, sensitivities, and treatment histories.
/// </summary>
[ApiController]
[Route("api/v1/beauty-profile")]
public sealed class BeautyProfileController : ControllerBase
{
    private readonly BeautyProfileService _beautyProfileService;

    public BeautyProfileController(BeautyProfileService beautyProfileService)
    {
        _beautyProfileService = beautyProfileService;
    }

    /// <summary>Returns the beauty profile of a customer.</summary>
    [HttpGet("{customerId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<BeautyProfileDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByCustomerId(Guid customerId, CancellationToken cancellationToken = default)
    {
        var profile = await _beautyProfileService.GetByCustomerIdAsync(customerId, cancellationToken);
        return Ok(ApiResponse<BeautyProfileDto>.CreateSuccess(profile, "Beauty profile retrieved successfully."));
    }

    /// <summary>Creates or updates the beauty profile of a customer.</summary>
    [HttpPut("{customerId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<BeautyProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upsert(
        Guid customerId,
        [FromBody] UpdateBeautyProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var updated = await _beautyProfileService.UpsertAsync(customerId, request, cancellationToken);
            return Ok(ApiResponse<BeautyProfileDto>.CreateSuccess(updated, "Beauty profile saved successfully."));
        }
        catch (ValidationException ex)
        {
            return BadRequest(new ApiResponse<object?>(null, false, ex.Message));
        }
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/BarberSalon.IntegrationTests --filter "FullyQualifiedName~BeautyProfileControllerTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/BarberSalon.API/Controllers/BeautyProfileController.cs tests/BarberSalon.IntegrationTests/BeautyProfile/
git commit -m "feat(api): add BeautyProfileController and integration tests"
```

---

### Task 7: Reminders API Controller & Integration Tests

**Files:**
- Create: `src/BarberSalon.API/Controllers/RemindersController.cs`
- Create: `tests/BarberSalon.IntegrationTests/Reminders/RemindersControllerTests.cs`

**Interfaces:**
- Consumes: `BarberSalon.Application.Reminders.Services.ReminderService`
- Produces:
  - `GET /api/v1/reminders/rules` -> `ApiResponse<List<ReminderRuleDto>>`
  - `POST /api/v1/reminders/rules` -> `ApiResponse<ReminderRuleDto>` (201 Created)
  - `PATCH /api/v1/reminders/rules/{id:guid}/toggle` -> `ApiResponse<ReminderRuleDto>`
  - `DELETE /api/v1/reminders/rules/{id:guid}` -> `ApiResponse<object?>`

- [ ] **Step 1: Write the failing integration test**

```csharp
// tests/BarberSalon.IntegrationTests/Reminders/RemindersControllerTests.cs
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BarberSalon.Application.Reminders.DTOs;
using BarberSalon.Infrastructure.Persistence;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Xunit;

namespace BarberSalon.IntegrationTests.Reminders;

public class RemindersControllerTests : IClassFixture<BarberSalonWebFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public RemindersControllerTests(BarberSalonWebFactory factory)
    {
        _client = factory.GetClient();
    }

    [Fact]
    public async Task CreateRule_WithValidPayload_Returns201AndCreatesRule()
    {
        var request = new CreateReminderRuleRequest(
            Name: "یادآور ۳ هفته‌ای کوتاهی",
            Trigger: "21_days_after_haircut",
            Channel: "sms",
            MessageTemplate: "سلام {name} عزیز، وقت اصلاح مجدد شما فرا رسیده است.");

        var response = await _client.PostAsJsonAsync("/api/v1/reminders/rules", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<ReminderRuleDto>>(JsonOptions);
        envelope!.Success.Should().BeTrue();
        envelope.Data.Name.Should().Be("یادآور ۳ هفته‌ای کوتاهی");
        envelope.Data.IsActive.Should().BeTrue();
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/BarberSalon.IntegrationTests --filter "FullyQualifiedName~RemindersControllerTests"`
Expected: FAIL with 404 NotFound.

- [ ] **Step 3: Implement RemindersController**

```csharp
// src/BarberSalon.API/Controllers/RemindersController.cs
using BarberSalon.API.Common;
using BarberSalon.Application.Reminders.DTOs;
using BarberSalon.Application.Reminders.Services;
using Microsoft.AspNetCore.Mvc;

namespace BarberSalon.API.Controllers;

/// <summary>
/// Manages automated SMS reminder rules and engagement schedules.
/// </summary>
[ApiController]
[Route("api/v1/reminders")]
public sealed class RemindersController : ControllerBase
{
    private readonly ReminderService _reminderService;

    public RemindersController(ReminderService reminderService)
    {
        _reminderService = reminderService;
    }

    /// <summary>Returns configured reminder rules.</summary>
    [HttpGet("rules")]
    [ProducesResponseType(typeof(ApiResponse<List<ReminderRuleDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRules(CancellationToken cancellationToken = default)
    {
        var rules = await _reminderService.GetRulesAsync(cancellationToken);
        return Ok(ApiResponse<List<ReminderRuleDto>>.CreateSuccess(rules, "Reminder rules retrieved successfully."));
    }

    /// <summary>Creates a new reminder rule.</summary>
    [HttpPost("rules")]
    [ProducesResponseType(typeof(ApiResponse<ReminderRuleDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateRule(
        [FromBody] CreateReminderRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        var created = await _reminderService.CreateRuleAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<ReminderRuleDto>.CreateSuccess(created, "Reminder rule created successfully."));
    }

    /// <summary>Toggles a reminder rule active state.</summary>
    [HttpPatch("rules/{id:guid}/toggle")]
    [ProducesResponseType(typeof(ApiResponse<ReminderRuleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleRule(Guid id, CancellationToken cancellationToken = default)
    {
        var toggled = await _reminderService.ToggleRuleAsync(id, cancellationToken);
        if (toggled is null)
        {
            return NotFound(new ApiResponse<object?>(null, false, "Reminder rule not found."));
        }
        return Ok(ApiResponse<ReminderRuleDto>.CreateSuccess(toggled, "Reminder rule toggled successfully."));
    }

    /// <summary>Deletes a reminder rule.</summary>
    [HttpDelete("rules/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRule(Guid id, CancellationToken cancellationToken = default)
    {
        var deleted = await _reminderService.DeleteRuleAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound(new ApiResponse<object?>(null, false, "Reminder rule not found."));
        }
        return Ok(ApiResponse<object?>.CreateSuccess(null, "Reminder rule deleted successfully."));
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/BarberSalon.IntegrationTests --filter "FullyQualifiedName~RemindersControllerTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/BarberSalon.API/Controllers/RemindersController.cs tests/BarberSalon.IntegrationTests/Reminders/
git commit -m "feat(api): add RemindersController and integration tests"
```

---

### Task 8: Portfolio / Gallery API Controller & Integration Tests

**Files:**
- Create: `src/BarberSalon.API/Controllers/PortfolioController.cs`
- Create: `tests/BarberSalon.IntegrationTests/Portfolio/PortfolioControllerTests.cs`

**Interfaces:**
- Consumes: `BarberSalon.Application.Portfolio.Services.PortfolioService`
- Produces:
  - `GET /api/v1/portfolio` -> `ApiResponse<List<PortfolioItemDto>>`
  - `GET /api/v1/portfolio/{id:guid}` -> `ApiResponse<PortfolioItemDto>`
  - `POST /api/v1/portfolio` -> `ApiResponse<PortfolioItemDto>` (201 Created)
  - `PUT /api/v1/portfolio/{id:guid}` -> `ApiResponse<PortfolioItemDto>`
  - `DELETE /api/v1/portfolio/{id:guid}` -> `ApiResponse<object?>`

- [ ] **Step 1: Write the failing integration test**

```csharp
// tests/BarberSalon.IntegrationTests/Portfolio/PortfolioControllerTests.cs
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BarberSalon.Application.Portfolio.DTOs;
using BarberSalon.Infrastructure.Persistence;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Xunit;

namespace BarberSalon.IntegrationTests.Portfolio;

public class PortfolioControllerTests : IClassFixture<BarberSalonWebFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public PortfolioControllerTests(BarberSalonWebFactory factory)
    {
        _client = factory.GetClient();
    }

    [Fact]
    public async Task CreatePortfolioItem_WithValidData_Returns201AndPersistsItem()
    {
        var request = new CreatePortfolioItemRequest(
            Title: "گریم داماد VIP",
            Category: "groom",
            BeforeImageUrl: "https://example.com/before.jpg",
            AfterImageUrl: "https://example.com/after.jpg",
            Description: "پکیج کامل پاکسازی و استایل داماد",
            StaffId: Guid.NewGuid());

        var response = await _client.PostAsJsonAsync("/api/v1/portfolio", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<PortfolioItemDto>>(JsonOptions);
        envelope!.Success.Should().BeTrue();
        envelope.Data.Title.Should().Be("گریم داماد VIP");
        envelope.Data.Category.Should().Be("groom");
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/BarberSalon.IntegrationTests --filter "FullyQualifiedName~PortfolioControllerTests"`
Expected: FAIL with 404 NotFound.

- [ ] **Step 3: Implement PortfolioController**

```csharp
// src/BarberSalon.API/Controllers/PortfolioController.cs
using BarberSalon.API.Common;
using BarberSalon.Application.Portfolio.DTOs;
using BarberSalon.Application.Portfolio.Services;
using Microsoft.AspNetCore.Mvc;

namespace BarberSalon.API.Controllers;

/// <summary>
/// Manages before-and-after showcase portfolio items and salon gallery records.
/// </summary>
[ApiController]
[Route("api/v1/portfolio")]
public sealed class PortfolioController : ControllerBase
{
    private readonly PortfolioService _portfolioService;

    public PortfolioController(PortfolioService portfolioService)
    {
        _portfolioService = portfolioService;
    }

    /// <summary>Returns portfolio items filtered by category or staff member.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<PortfolioItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? category = null,
        [FromQuery] Guid? staffId = null,
        CancellationToken cancellationToken = default)
    {
        var items = await _portfolioService.GetAllAsync(category, staffId, cancellationToken);
        return Ok(ApiResponse<List<PortfolioItemDto>>.CreateSuccess(items, "Portfolio items retrieved successfully."));
    }

    /// <summary>Returns a specific portfolio item by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PortfolioItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _portfolioService.GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return NotFound(new ApiResponse<object?>(null, false, "Portfolio item not found."));
        }
        return Ok(ApiResponse<PortfolioItemDto>.CreateSuccess(item, "Portfolio item retrieved successfully."));
    }

    /// <summary>Creates a new portfolio showcase entry.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PortfolioItemDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePortfolioItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var created = await _portfolioService.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<PortfolioItemDto>.CreateSuccess(created, "Portfolio item created successfully."));
    }

    /// <summary>Updates an existing portfolio entry.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PortfolioItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdatePortfolioItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var updated = await _portfolioService.UpdateAsync(id, request, cancellationToken);
        if (updated is null)
        {
            return NotFound(new ApiResponse<object?>(null, false, "Portfolio item not found."));
        }
        return Ok(ApiResponse<PortfolioItemDto>.CreateSuccess(updated, "Portfolio item updated successfully."));
    }

    /// <summary>Deletes a portfolio entry.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var deleted = await _portfolioService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound(new ApiResponse<object?>(null, false, "Portfolio item not found."));
        }
        return Ok(ApiResponse<object?>.CreateSuccess(null, "Portfolio item deleted successfully."));
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/BarberSalon.IntegrationTests --filter "FullyQualifiedName~PortfolioControllerTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/BarberSalon.API/Controllers/PortfolioController.cs tests/BarberSalon.IntegrationTests/Portfolio/
git commit -m "feat(api): add PortfolioController and integration tests"
```

---

### Task 9: Staff Performance API Controller & Integration Tests

**Files:**
- Create: `src/BarberSalon.API/Controllers/StaffPerformanceController.cs`
- Create: `tests/BarberSalon.IntegrationTests/StaffPerformance/StaffPerformanceControllerTests.cs`

**Interfaces:**
- Consumes: `BarberSalon.Application.StaffPerformance.Services.StaffPerformanceService`
- Produces: `GET /api/v1/staff-performance` -> `ApiResponse<List<StaffPerformanceDto>>`

- [ ] **Step 1: Write the failing integration test**

```csharp
// tests/BarberSalon.IntegrationTests/StaffPerformance/StaffPerformanceControllerTests.cs
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BarberSalon.Application.StaffPerformance.DTOs;
using BarberSalon.Domain.Staff.Entities;
using BarberSalon.Infrastructure.Persistence;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BarberSalon.IntegrationTests.StaffPerformance;

public class StaffPerformanceControllerTests : IClassFixture<BarberSalonWebFactory>
{
    private readonly HttpClient _client;
    private readonly BarberSalonWebFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public StaffPerformanceControllerTests(BarberSalonWebFactory factory)
    {
        _factory = factory;
        _client = factory.GetClient();
    }

    [Fact]
    public async Task GetStaffPerformance_WhenCalled_Returns200WithRankedMetrics()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var staff = StaffMember.Create("آرایشگر ممتاز", "ممتاز", "09121113344", "استاد کوتاهی", "Barber", 5);
            db.StaffMembers.Add(staff);
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync("/api/v1/staff-performance");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<List<StaffPerformanceDto>>>(JsonOptions);
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/BarberSalon.IntegrationTests --filter "FullyQualifiedName~StaffPerformanceControllerTests"`
Expected: FAIL with 404 NotFound.

- [ ] **Step 3: Implement StaffPerformanceController**

```csharp
// src/BarberSalon.API/Controllers/StaffPerformanceController.cs
using BarberSalon.API.Common;
using BarberSalon.Application.StaffPerformance.DTOs;
using BarberSalon.Application.StaffPerformance.Services;
using Microsoft.AspNetCore.Mvc;

namespace BarberSalon.API.Controllers;

/// <summary>
/// Provides calculated performance, revenue, rating, and cancellation metrics for salon staff members.
/// </summary>
[ApiController]
[Route("api/v1/staff-performance")]
public sealed class StaffPerformanceController : ControllerBase
{
    private readonly StaffPerformanceService _staffPerformanceService;

    public StaffPerformanceController(StaffPerformanceService staffPerformanceService)
    {
        _staffPerformanceService = staffPerformanceService;
    }

    /// <summary>Returns ranked staff metrics for HR and performance reviews.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<StaffPerformanceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPerformance(
        [FromQuery] string? from = null,
        [FromQuery] string? to = null,
        CancellationToken cancellationToken = default)
    {
        var metrics = await _staffPerformanceService.GetStaffPerformanceAsync(from, to, cancellationToken);
        return Ok(ApiResponse<List<StaffPerformanceDto>>.CreateSuccess(metrics, "Staff performance metrics retrieved successfully."));
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/BarberSalon.IntegrationTests --filter "FullyQualifiedName~StaffPerformanceControllerTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/BarberSalon.API/Controllers/StaffPerformanceController.cs tests/BarberSalon.IntegrationTests/StaffPerformance/
git commit -m "feat(api): add StaffPerformanceController and integration tests"
```

---

### Task 10: Frontend Integration & End-to-End Verification

**Files:**
- Modify: `beauty-saloon-front-V2/src/app/admin/appointments/page.tsx`
- Modify: `beauty-saloon-front-V2/src/app/admin/staff/page.tsx`
- Modify: `beauty-saloon-front-V2/src/app/admin/customers/[id]/CustomerProfileClient.tsx`
- Modify: `beauty-saloon-front-V2/src/app/(auth)/login/page.tsx`

**Interfaces:**
- Connect remaining dropdowns and forms to RTK Query hooks (`useGetStaffQuery`, `useGetServicesQuery`, `useUpdateUserProfileMutation`).

- [ ] **Step 1: Replace Redux staff select with `useGetStaffQuery` in `AppointmentsPage`**

In `beauty-saloon-front-V2/src/app/admin/appointments/page.tsx`:
Replace `const staffList = useAppSelector(selectAdminStaff);` with:
```typescript
const { data: remoteStaff } = useGetStaffQuery();
const reduxStaff = useAppSelector(selectAdminStaff);
const staffList = remoteStaff ?? reduxStaff;
```

- [ ] **Step 2: Replace Redux services select with `useGetServicesQuery` in `StaffPage`**

In `beauty-saloon-front-V2/src/app/admin/staff/page.tsx`:
Replace `const services = useAppSelector(selectAdminServices);` with:
```typescript
const { data: remoteServices } = useGetServicesQuery();
const reduxServices = useAppSelector(selectAdminServices);
const services = remoteServices ?? reduxServices;
```

- [ ] **Step 3: Run frontend build and typecheck**

Run: `npm run build` in `beauty-saloon-front-V2`
Expected: Zero type errors, build successful.

- [ ] **Step 4: Run all backend integration tests**

Run: `dotnet test tests/BarberSalon.IntegrationTests`
Expected: 100% tests passing green.

- [ ] **Step 5: Commit**

```bash
git add src/app/admin/appointments/page.tsx src/app/admin/staff/page.tsx
git commit -m "feat(front): wire live RTK Query hooks and eliminate local mock fallbacks"
```
