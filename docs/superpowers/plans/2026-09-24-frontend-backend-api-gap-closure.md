# Frontend-Backend API Gap Closure Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the 9 missing ASP.NET Core API controllers in `BarberSalon.API` to eliminate all frontend integration gaps and replace client mock fallbacks with real database-backed endpoints.

**Architecture:** Build thin, RESTful API controllers in `BarberSalon.API/Controllers/` following the existing `ApiResponse<T>` envelope convention. Each controller directly injects its pre-existing Application service (`DashboardService`, `UserService`, `ReviewService`, `WaitlistService`, `LoyaltyService`, `ReminderService`, `PortfolioService`, `BeautyProfileService`, `StaffPerformanceService`) and handles error responses using existing `ValidationException` and `NotFoundException`. Write automated integration tests for every endpoint using `BarberSalonWebFactory` and in-memory EF Core.

**Tech Stack:** .NET 10, ASP.NET Core Web API, C# 13, Entity Framework Core, xUnit, FluentAssertions, Microsoft.AspNetCore.Mvc.Testing.

**Spec:** [`e:/barber/beauty-saloon-front-V2/docs/PROJECT_DOCUMENTATION.md`](file:///e:/barber/beauty-saloon-front-V2/docs/PROJECT_DOCUMENTATION.md) and [`e:/barber/beauty-saloon-api/CONTEXT.md`](file:///e:/barber/beauty-saloon-api/CONTEXT.md).

## Global Constraints
- Target Framework: `net10.0`
- Response Envelope: Must use `ApiResponse<T>.CreateSuccess(data, message)` on 200/201 and `ApiResponse<object?>` on 400/404.
- Route Prefix: `api/v1/{resource}`
- JSON serialization: camelCase matching RTK Query contracts in `beauty-saloon-front-V2/src/store/api/`.
- TDD Protocol: Every task requires writing the integration test first, verifying red failure, writing minimal controller code, and confirming green pass.

---

### Task 1: DashboardController (`GET /api/v1/dashboard/stats`)

**Files:**
- Create: `src/BarberSalon.API/Controllers/DashboardController.cs`
- Test: `tests/BarberSalon.IntegrationTests/Admin/DashboardStatsIntegrationTests.cs`

**Interfaces:**
- Consumes: `DashboardService.GetDashboardStatsAsync(CancellationToken)` in `BarberSalon.Application.Admin.Services`
- Produces: `GET /api/v1/dashboard/stats` returning `ApiResponse<DashboardStatsDto>`

- [ ] **Step 1: Write the failing integration test**

Create `tests/BarberSalon.IntegrationTests/Admin/DashboardStatsIntegrationTests.cs`:
```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BarberSalon.Application.Admin.DTOs;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Xunit;

namespace BarberSalon.IntegrationTests.Admin;

public class DashboardStatsIntegrationTests : IClassFixture<BarberSalonWebFactory>
{
    private const string Endpoint = "/api/v1/dashboard/stats";
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public DashboardStatsIntegrationTests(BarberSalonWebFactory factory)
    {
        _client = factory.GetClient();
    }

    [Fact]
    public async Task GetStats_Returns200WithDashboardStatsEnvelope()
    {
        // Act
        var response = await _client.GetAsync(Endpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<DashboardStatsDto>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Message.Should().Be("Dashboard statistics retrieved successfully.");
        envelope.Data.Should().NotBeNull();
        envelope.Data!.TodayAppointments.Should().BeGreaterOrEqualTo(0);
        envelope.Data.PendingAppointments.Should().BeGreaterOrEqualTo(0);
        envelope.Data.WeeklyCustomers.Should().BeGreaterOrEqualTo(0);
        envelope.Data.TodayRevenue.Should().BeGreaterOrEqualTo(0);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/BarberSalon.IntegrationTests --filter "FullyQualifiedName~DashboardStatsIntegrationTests"`
Expected: FAIL with HTTP 404 (NotFound).

- [ ] **Step 3: Write minimal implementation**

Create `src/BarberSalon.API/Controllers/DashboardController.cs`:
```csharp
using BarberSalon.API.Common;
using BarberSalon.Application.Admin.DTOs;
using BarberSalon.Application.Admin.Services;
using Microsoft.AspNetCore.Mvc;

namespace BarberSalon.API.Controllers;

/// <summary>
/// Provides operational statistics and key performance indicators for the salon dashboard.
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

    /// <summary>
    /// Returns aggregated statistics for today's salon operations.
    /// </summary>
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

Run: `dotnet test tests/BarberSalon.IntegrationTests --filter "FullyQualifiedName~DashboardStatsIntegrationTests"`
Expected: PASS (Status 200 with JSON envelope).

- [ ] **Step 5: Commit**

```bash
git add src/BarberSalon.API/Controllers/DashboardController.cs tests/BarberSalon.IntegrationTests/Admin/DashboardStatsIntegrationTests.cs
git commit -m "feat(api): add DashboardController for dashboard statistics endpoint"
```

---

### Task 2: UsersController (`GET /api/v1/users/{id}` & `PUT /api/v1/users/{id}/profile`)

**Files:**
- Create: `src/BarberSalon.API/Controllers/UsersController.cs`
- Test: `tests/BarberSalon.IntegrationTests/Auth/UsersIntegrationTests.cs`

**Interfaces:**
- Consumes: `UserService.GetByIdAsync` and `UserService.UpdateProfileAsync` in `BarberSalon.Application.Auth.Services`
- Produces: `GET /api/v1/users/{id:guid}` and `PUT /api/v1/users/{id:guid}/profile`

- [ ] **Step 1: Write the failing integration test**

Create `tests/BarberSalon.IntegrationTests/Auth/UsersIntegrationTests.cs`:
```csharp
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

namespace BarberSalon.IntegrationTests.Auth;

public class UsersIntegrationTests : IClassFixture<BarberSalonWebFactory>
{
    private const string BaseEndpoint = "/api/v1/users";
    private readonly HttpClient _client;
    private readonly BarberSalonWebFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public UsersIntegrationTests(BarberSalonWebFactory factory)
    {
        _factory = factory;
        _client = factory.GetClient();
    }

    [Fact]
    public async Task GetById_WhenUserExists_Returns200WithUserDto()
    {
        // Arrange
        Guid userId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = User.Create("09121112233", "کاربر تست", UserRole.Customer);
            db.Users.Add(user);
            await db.SaveChangesAsync();
            userId = user.Id;
        }

        // Act
        var response = await _client.GetAsync($"{BaseEndpoint}/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<UserDto>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data!.Name.Should().Be("کاربر تست");
        envelope.Data.PhoneNumber.Should().Be("09121112233");
    }

    [Fact]
    public async Task UpdateProfile_WhenValid_Returns200WithUpdatedName()
    {
        // Arrange
        Guid userId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = User.Create("09124445566", "نام اولیه", UserRole.Customer);
            db.Users.Add(user);
            await db.SaveChangesAsync();
            userId = user.Id;
        }

        var updatePayload = new UpdateUserProfileRequest("نام جدید بروز شده");

        // Act
        var response = await _client.PutAsJsonAsync($"{BaseEndpoint}/{userId}/profile", updatePayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<UserDto>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data!.Name.Should().Be("نام جدید بروز شده");
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/BarberSalon.IntegrationTests --filter "FullyQualifiedName~UsersIntegrationTests"`
Expected: FAIL with HTTP 404 (NotFound).

- [ ] **Step 3: Write minimal implementation**

Create `src/BarberSalon.API/Controllers/UsersController.cs`:
```csharp
using BarberSalon.API.Common;
using BarberSalon.Application.Auth.DTOs;
using BarberSalon.Application.Auth.Services;
using BarberSalon.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace BarberSalon.API.Controllers;

/// <summary>
/// Manages user account profiles and name details.
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

    /// <summary>
    /// Returns a user profile by unique identifier.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userService.GetByIdAsync(id, cancellationToken);
            return Ok(ApiResponse<UserDto>.CreateSuccess(user, "User profile retrieved successfully."));
        }
        catch (NotFoundException ex)
        {
            return NotFound(new ApiResponse<object?>(null, false, ex.Message));
        }
    }

    /// <summary>
    /// Updates user profile display name.
    /// </summary>
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
            return Ok(ApiResponse<UserDto>.CreateSuccess(updated, "User profile updated successfully."));
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

Run: `dotnet test tests/BarberSalon.IntegrationTests --filter "FullyQualifiedName~UsersIntegrationTests"`
Expected: PASS (Both tests pass green).

- [ ] **Step 5: Commit**

```bash
git add src/BarberSalon.API/Controllers/UsersController.cs tests/BarberSalon.IntegrationTests/Auth/UsersIntegrationTests.cs
git commit -m "feat(api): add UsersController for user profile lookup and update"
```

---

### Task 3: ReviewsController (`GET`, `POST`, `PUT status`, `DELETE`)

**Files:**
- Create: `src/BarberSalon.API/Controllers/ReviewsController.cs`
- Test: `tests/BarberSalon.IntegrationTests/Reviews/ReviewsIntegrationTests.cs`

**Interfaces:**
- Consumes: `ReviewService` in `BarberSalon.Application.Reviews.Services`
- Produces: `GET /api/v1/reviews`, `GET /api/v1/reviews/staff/{staffId}`, `POST /api/v1/reviews`, `PUT /api/v1/reviews/{id}/status`, `DELETE /api/v1/reviews/{id}`

- [ ] **Step 1: Write the failing integration test**

Create `tests/BarberSalon.IntegrationTests/Reviews/ReviewsIntegrationTests.cs`:
```csharp
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
    private const string BaseEndpoint = "/api/v1/reviews";
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ReviewsIntegrationTests(BarberSalonWebFactory factory)
    {
        _client = factory.GetClient();
    }

    [Fact]
    public async Task CreateAndRetrieveReview_SucceedsWith200And201()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var createRequest = new CreateReviewRequest(
            CustomerId: customerId,
            CustomerName: "مشتری رضایتمند",
            Rating: 5,
            Comment: "کار بسیار عالی و تمیز بود",
            StaffId: staffId,
            StaffName: "رضا محمدی",
            ServiceId: serviceId,
            ServiceName: "کوتاهی کلاسیک",
            AppointmentId: Guid.NewGuid());

        // Act - Create
        var postResponse = await _client.PostAsJsonAsync(BaseEndpoint, createRequest);
        postResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdEnvelope = await postResponse.Content.ReadFromJsonAsync<ApiResponseEnvelope<ReviewDto>>(JsonOptions);
        createdEnvelope.Should().NotBeNull();
        createdEnvelope!.Data.Should().NotBeNull();
        createdEnvelope.Data!.Rating.Should().Be(5);
        createdEnvelope.Data.Comment.Should().Be("کار بسیار عالی و تمیز بود");

        // Act - Get By Staff
        var getStaffResponse = await _client.GetAsync($"{BaseEndpoint}/staff/{staffId}");
        getStaffResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var staffReviews = await getStaffResponse.Content.ReadFromJsonAsync<ApiResponseEnvelope<List<ReviewDto>>>(JsonOptions);
        staffReviews!.Data.Should().Contain(r => r.Id == createdEnvelope.Data.Id);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/BarberSalon.IntegrationTests --filter "FullyQualifiedName~ReviewsIntegrationTests"`
Expected: FAIL with HTTP 404 (NotFound).

- [ ] **Step 3: Write minimal implementation**

Create `src/BarberSalon.API/Controllers/ReviewsController.cs`:
```csharp
using BarberSalon.API.Common;
using BarberSalon.Application.Common.Exceptions;
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

    /// <summary>
    /// Returns all reviews matching query filters.
    /// </summary>
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

    /// <summary>
    /// Returns all reviews for a specific staff member.
    /// </summary>
    [HttpGet("staff/{staffId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<List<ReviewDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByStaffId(Guid staffId, CancellationToken cancellationToken = default)
    {
        var reviews = await _reviewService.GetByStaffIdAsync(staffId, cancellationToken);
        return Ok(ApiResponse<List<ReviewDto>>.CreateSuccess(reviews, "Staff reviews retrieved successfully."));
    }

    /// <summary>
    /// Submits a new customer review.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ReviewDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var review = await _reviewService.CreateAsync(request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, ApiResponse<ReviewDto>.CreateSuccess(review, "Review submitted successfully."));
        }
        catch (ValidationException ex)
        {
            return BadRequest(new ApiResponse<object?>(null, false, ex.Message));
        }
    }

    /// <summary>
    /// Updates review status (e.g. published, hidden).
    /// </summary>
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

    /// <summary>
    /// Deletes a review.
    /// </summary>
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

public sealed record UpdateReviewStatusRequest(string Status);
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/BarberSalon.IntegrationTests --filter "FullyQualifiedName~ReviewsIntegrationTests"`
Expected: PASS (Status 201 Created and Status 200 OK).

- [ ] **Step 5: Commit**

```bash
git add src/BarberSalon.API/Controllers/ReviewsController.cs tests/BarberSalon.IntegrationTests/Reviews/ReviewsIntegrationTests.cs
git commit -m "feat(api): add ReviewsController with CRUD and moderation endpoints"
```

---

### Task 4: WaitlistController (`GET`, `POST`, `POST notify`, `DELETE`)

**Files:**
- Create: `src/BarberSalon.API/Controllers/WaitlistController.cs`
- Test: `tests/BarberSalon.IntegrationTests/Waitlist/WaitlistIntegrationTests.cs`

**Interfaces:**
- Consumes: `WaitlistService` in `BarberSalon.Application.Waitlist.Services`
- Produces: `GET /api/v1/waitlist`, `POST /api/v1/waitlist`, `POST /api/v1/waitlist/{id}/notify`, `DELETE /api/v1/waitlist/{id}`

- [ ] **Step 1: Write the failing integration test**

Create `tests/BarberSalon.IntegrationTests/Waitlist/WaitlistIntegrationTests.cs`:
```csharp
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
    private const string BaseEndpoint = "/api/v1/waitlist";
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public WaitlistIntegrationTests(BarberSalonWebFactory factory)
    {
        _client = factory.GetClient();
    }

    [Fact]
    public async Task JoinWaitlist_And_Notify_Succeeds()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var joinRequest = new JoinWaitlistRequest(
            CustomerId: customerId,
            CustomerName: "سارا کریمی",
            CustomerPhone: "09127778899",
            StaffId: staffId,
            StaffName: "مهسا بهرامی",
            ServiceId: serviceId,
            ServiceName: "رنگ مو",
            Date: "1405/01/20",
            Time: "11:00");

        // Act - Join
        var response = await _client.PostAsJsonAsync(BaseEndpoint, joinRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var entryEnvelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<WaitlistEntryDto>>(JsonOptions);
        entryEnvelope.Should().NotBeNull();
        entryEnvelope!.Data.Should().NotBeNull();
        var entryId = entryEnvelope.Data!.Id;

        // Act - Notify
        var notifyResponse = await _client.PostAsync($"{BaseEndpoint}/{entryId}/notify", null);
        notifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var notifiedEnvelope = await notifyResponse.Content.ReadFromJsonAsync<ApiResponseEnvelope<WaitlistEntryDto>>(JsonOptions);
        notifiedEnvelope!.Data!.Status.Should().Be("notified");
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/BarberSalon.IntegrationTests --filter "FullyQualifiedName~WaitlistIntegrationTests"`
Expected: FAIL with HTTP 404 (NotFound).

- [ ] **Step 3: Write minimal implementation**

Create `src/BarberSalon.API/Controllers/WaitlistController.cs`:
```csharp
using BarberSalon.API.Common;
using BarberSalon.Application.Common.Exceptions;
using BarberSalon.Application.Waitlist.DTOs;
using BarberSalon.Application.Waitlist.Services;
using Microsoft.AspNetCore.Mvc;

namespace BarberSalon.API.Controllers;

/// <summary>
/// Manages customer waitlist entries and notification triggers.
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

    /// <summary>
    /// Returns waitlist entries filtered by status, staff, or date.
    /// </summary>
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

    /// <summary>
    /// Joins the waitlist for a specific staff member and time slot.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<WaitlistEntryDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Join(
        [FromBody] JoinWaitlistRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entry = await _waitlistService.JoinAsync(request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, ApiResponse<WaitlistEntryDto>.CreateSuccess(entry, "Joined waitlist successfully."));
        }
        catch (ValidationException ex)
        {
            return BadRequest(new ApiResponse<object?>(null, false, ex.Message));
        }
    }

    /// <summary>
    /// Sends notification for an available slot to the waitlisted customer.
    /// </summary>
    [HttpPost("{id:guid}/notify")]
    [ProducesResponseType(typeof(ApiResponse<WaitlistEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Notify(Guid id, CancellationToken cancellationToken = default)
    {
        var entry = await _waitlistService.NotifyAsync(id, cancellationToken);
        if (entry is null)
        {
            return NotFound(new ApiResponse<object?>(null, false, "Waitlist entry not found."));
        }
        return Ok(ApiResponse<WaitlistEntryDto>.CreateSuccess(entry, "Customer notified successfully."));
    }

    /// <summary>
    /// Cancels or removes a waitlist entry.
    /// </summary>
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
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/BarberSalon.IntegrationTests --filter "FullyQualifiedName~WaitlistIntegrationTests"`
Expected: PASS (Status 201 Created and Status 200 OK).

- [ ] **Step 5: Commit**

```bash
git add src/BarberSalon.API/Controllers/WaitlistController.cs tests/BarberSalon.IntegrationTests/Waitlist/WaitlistIntegrationTests.cs
git commit -m "feat(api): add WaitlistController for queueing and notifications"
```

---

### Task 5: LoyaltyController (`accounts`, `referrals`, `validate-referral`, `apply-referral`)

**Files:**
- Create: `src/BarberSalon.API/Controllers/LoyaltyController.cs`
- Test: `tests/BarberSalon.IntegrationTests/Loyalty/LoyaltyIntegrationTests.cs`

**Interfaces:**
- Consumes: `LoyaltyService` in `BarberSalon.Application.Loyalty.Services`
- Produces: `GET /api/v1/loyalty/accounts/{customerId}`, `GET /api/v1/loyalty/referrals`, `GET /api/v1/loyalty/validate-referral`, `POST /api/v1/loyalty/apply-referral`

- [ ] **Step 1: Write the failing integration test**

Create `tests/BarberSalon.IntegrationTests/Loyalty/LoyaltyIntegrationTests.cs`:
```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BarberSalon.Application.Loyalty.DTOs;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Xunit;

namespace BarberSalon.IntegrationTests.Loyalty;

public class LoyaltyIntegrationTests : IClassFixture<BarberSalonWebFactory>
{
    private const string BaseEndpoint = "/api/v1/loyalty";
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public LoyaltyIntegrationTests(BarberSalonWebFactory factory)
    {
        _client = factory.GetClient();
    }

    [Fact]
    public async Task GetOrCreateAccount_Returns200WithLoyaltyAccount()
    {
        // Arrange
        var customerId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"{BaseEndpoint}/accounts/{customerId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<LoyaltyAccountDto>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.CustomerId.Should().Be(customerId);
        envelope.Data.Tier.Should().Be("Bronze");
    }

    [Fact]
    public async Task ValidateReferralCode_WhenCodeIsEmpty_ReturnsInvalidResponse()
    {
        // Act
        var response = await _client.GetAsync($"{BaseEndpoint}/validate-referral?code=");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<ValidateReferralResponse>>(JsonOptions);
        envelope!.Data!.Valid.Should().BeFalse();
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/BarberSalon.IntegrationTests --filter "FullyQualifiedName~LoyaltyIntegrationTests"`
Expected: FAIL with HTTP 404 (NotFound).

- [ ] **Step 3: Write minimal implementation**

Create `src/BarberSalon.API/Controllers/LoyaltyController.cs`:
```csharp
using BarberSalon.API.Common;
using BarberSalon.Application.Loyalty.DTOs;
using BarberSalon.Application.Loyalty.Services;
using Microsoft.AspNetCore.Mvc;

namespace BarberSalon.API.Controllers;

/// <summary>
/// Manages customer loyalty points, rewards, and referral programs.
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

    /// <summary>
    /// Returns or creates a customer's loyalty account.
    /// </summary>
    [HttpGet("accounts/{customerId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<LoyaltyAccountDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAccount(Guid customerId, CancellationToken cancellationToken = default)
    {
        var account = await _loyaltyService.GetOrCreateAccountAsync(customerId, cancellationToken);
        return Ok(ApiResponse<LoyaltyAccountDto>.CreateSuccess(account, "Loyalty account retrieved successfully."));
    }

    /// <summary>
    /// Returns referral records, optionally filtered by customer identifier.
    /// </summary>
    [HttpGet("referrals")]
    [ProducesResponseType(typeof(ApiResponse<List<ReferralDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReferrals([FromQuery] Guid? customerId = null, CancellationToken cancellationToken = default)
    {
        var referrals = await _loyaltyService.GetReferralsAsync(customerId, cancellationToken);
        return Ok(ApiResponse<List<ReferralDto>>.CreateSuccess(referrals, "Referrals retrieved successfully."));
    }

    /// <summary>
    /// Validates a customer referral code and returns discount terms.
    /// </summary>
    [HttpGet("validate-referral")]
    [ProducesResponseType(typeof(ApiResponse<ValidateReferralResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ValidateReferral(
        [FromQuery] string code,
        [FromQuery] Guid? customerId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _loyaltyService.ValidateReferralCodeAsync(code ?? string.Empty, customerId, cancellationToken);
        return Ok(ApiResponse<ValidateReferralResponse>.CreateSuccess(result, "Referral code validated."));
    }

    /// <summary>
    /// Applies a referral code to a booking transaction.
    /// </summary>
    [HttpPost("apply-referral")]
    [ProducesResponseType(typeof(ApiResponse<ReferralDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ApplyReferral(
        [FromBody] ApplyReferralRequest request,
        CancellationToken cancellationToken = default)
    {
        var referral = await _loyaltyService.ApplyReferralAsync(request, cancellationToken);
        return Ok(ApiResponse<ReferralDto>.CreateSuccess(referral, "Referral applied successfully."));
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/BarberSalon.IntegrationTests --filter "FullyQualifiedName~LoyaltyIntegrationTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/BarberSalon.API/Controllers/LoyaltyController.cs tests/BarberSalon.IntegrationTests/Loyalty/LoyaltyIntegrationTests.cs
git commit -m "feat(api): add LoyaltyController for points and referral validation"
```

---

### Task 6: RemindersController (`rules`, `POST`, `PATCH toggle`, `DELETE`)

**Files:**
- Create: `src/BarberSalon.API/Controllers/RemindersController.cs`
- Test: `tests/BarberSalon.IntegrationTests/Reminders/RemindersIntegrationTests.cs`

**Interfaces:**
- Consumes: `ReminderService` in `BarberSalon.Application.Reminders.Services`
- Produces: `GET /api/v1/reminders/rules`, `POST /api/v1/reminders/rules`, `PATCH /api/v1/reminders/rules/{id}/toggle`, `DELETE /api/v1/reminders/rules/{id}`

- [ ] **Step 1: Write the failing integration test**

Create `tests/BarberSalon.IntegrationTests/Reminders/RemindersIntegrationTests.cs`:
```csharp
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
    private const string BaseEndpoint = "/api/v1/reminders/rules";
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public RemindersIntegrationTests(BarberSalonWebFactory factory)
    {
        _client = factory.GetClient();
    }

    [Fact]
    public async Task CreateRule_And_Toggle_Succeeds()
    {
        // Arrange
        var request = new CreateReminderRuleRequest(
            Name: "یادآور ۲۴ ساعت قبل",
            Trigger: "24h_before",
            Channel: "sms",
            MessageTemplate: "سلام {customer}، نوبت شما فردا ساعت {time} است.");

        // Act - Create
        var postResponse = await _client.PostAsJsonAsync(BaseEndpoint, request);
        postResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await postResponse.Content.ReadFromJsonAsync<ApiResponseEnvelope<ReminderRuleDto>>(JsonOptions);
        created.Should().NotBeNull();
        created!.Data!.IsActive.Should().BeTrue();

        // Act - Toggle
        var toggleResponse = await _client.PatchAsync($"{BaseEndpoint}/{created.Data.Id}/toggle", null);
        toggleResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var toggled = await toggleResponse.Content.ReadFromJsonAsync<ApiResponseEnvelope<ReminderRuleDto>>(JsonOptions);
        toggled!.Data!.IsActive.Should().BeFalse();
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/BarberSalon.IntegrationTests --filter "FullyQualifiedName~RemindersIntegrationTests"`
Expected: FAIL with HTTP 404 (NotFound).

- [ ] **Step 3: Write minimal implementation**

Create `src/BarberSalon.API/Controllers/RemindersController.cs`:
```csharp
using BarberSalon.API.Common;
using BarberSalon.Application.Reminders.DTOs;
using BarberSalon.Application.Reminders.Services;
using Microsoft.AspNetCore.Mvc;

namespace BarberSalon.API.Controllers;

/// <summary>
/// Manages automated appointment and service reminder rules.
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

    /// <summary>
    /// Returns all reminder configuration rules.
    /// </summary>
    [HttpGet("rules")]
    [ProducesResponseType(typeof(ApiResponse<List<ReminderRuleDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRules(CancellationToken cancellationToken = default)
    {
        var rules = await _reminderService.GetRulesAsync(cancellationToken);
        return Ok(ApiResponse<List<ReminderRuleDto>>.CreateSuccess(rules, "Reminder rules retrieved successfully."));
    }

    /// <summary>
    /// Creates a new reminder rule.
    /// </summary>
    [HttpPost("rules")]
    [ProducesResponseType(typeof(ApiResponse<ReminderRuleDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateRule(
        [FromBody] CreateReminderRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        var rule = await _reminderService.CreateRuleAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<ReminderRuleDto>.CreateSuccess(rule, "Reminder rule created successfully."));
    }

    /// <summary>
    /// Toggles the active status of a reminder rule.
    /// </summary>
    [HttpPatch("rules/{id:guid}/toggle")]
    [ProducesResponseType(typeof(ApiResponse<ReminderRuleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleRule(Guid id, CancellationToken cancellationToken = default)
    {
        var rule = await _reminderService.ToggleRuleAsync(id, cancellationToken);
        if (rule is null)
        {
            return NotFound(new ApiResponse<object?>(null, false, "Reminder rule not found."));
        }
        return Ok(ApiResponse<ReminderRuleDto>.CreateSuccess(rule, "Reminder rule status toggled successfully."));
    }

    /// <summary>
    /// Deletes a reminder rule.
    /// </summary>
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

Run: `dotnet test tests/BarberSalon.IntegrationTests --filter "FullyQualifiedName~RemindersIntegrationTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/BarberSalon.API/Controllers/RemindersController.cs tests/BarberSalon.IntegrationTests/Reminders/RemindersIntegrationTests.cs
git commit -m "feat(api): add RemindersController for automated notification rules"
```

---

### Task 7: PortfolioController (`GET`, `POST`, `PUT`, `DELETE` on `/api/v1/portfolio`)

**Files:**
- Create: `src/BarberSalon.API/Controllers/PortfolioController.cs`
- Test: `tests/BarberSalon.IntegrationTests/Portfolio/PortfolioIntegrationTests.cs`

**Interfaces:**
- Consumes: `PortfolioService` in `BarberSalon.Application.Portfolio.Services`
- Produces: `GET /api/v1/portfolio`, `GET /api/v1/portfolio/{id}`, `POST /api/v1/portfolio`, `PUT /api/v1/portfolio/{id}`, `DELETE /api/v1/portfolio/{id}`

- [ ] **Step 1: Write the failing integration test**

Create `tests/BarberSalon.IntegrationTests/Portfolio/PortfolioIntegrationTests.cs`:
```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BarberSalon.Application.Portfolio.DTOs;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Xunit;

namespace BarberSalon.IntegrationTests.Portfolio;

public class PortfolioIntegrationTests : IClassFixture<BarberSalonWebFactory>
{
    private const string BaseEndpoint = "/api/v1/portfolio";
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public PortfolioIntegrationTests(BarberSalonWebFactory factory)
    {
        _client = factory.GetClient();
    }

    [Fact]
    public async Task CreatePortfolioItem_And_GetAll_Succeeds()
    {
        // Arrange
        var request = new CreatePortfolioItemRequest(
            Title: "گریم داماد حرفه‌ای",
            Category: "groom",
            BeforeImageUrl: "https://example.com/before.jpg",
            AfterImageUrl: "https://example.com/after.jpg",
            Description: "اصلاح و گریم کامل داماد",
            StaffId: Guid.NewGuid());

        // Act - Create
        var postResponse = await _client.PostAsJsonAsync(BaseEndpoint, request);
        postResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdEnvelope = await postResponse.Content.ReadFromJsonAsync<ApiResponseEnvelope<PortfolioItemDto>>(JsonOptions);
        createdEnvelope!.Data!.Title.Should().Be("گریم داماد حرفه‌ای");

        // Act - Get All
        var getResponse = await _client.GetAsync(BaseEndpoint);
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var listEnvelope = await getResponse.Content.ReadFromJsonAsync<ApiResponseEnvelope<List<PortfolioItemDto>>>(JsonOptions);
        listEnvelope!.Data.Should().Contain(p => p.Id == createdEnvelope.Data.Id);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/BarberSalon.IntegrationTests --filter "FullyQualifiedName~PortfolioIntegrationTests"`
Expected: FAIL with HTTP 404 (NotFound).

- [ ] **Step 3: Write minimal implementation**

Create `src/BarberSalon.API/Controllers/PortfolioController.cs`:
```csharp
using BarberSalon.API.Common;
using BarberSalon.Application.Portfolio.DTOs;
using BarberSalon.Application.Portfolio.Services;
using Microsoft.AspNetCore.Mvc;

namespace BarberSalon.API.Controllers;

/// <summary>
/// Manages salon portfolio before/after transformation gallery entries.
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

    /// <summary>
    /// Returns portfolio entries matching optional category or staff filters.
    /// </summary>
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

    /// <summary>
    /// Returns a single portfolio item by unique identifier.
    /// </summary>
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

    /// <summary>
    /// Creates a new portfolio transformation entry.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PortfolioItemDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePortfolioItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var item = await _portfolioService.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<PortfolioItemDto>.CreateSuccess(item, "Portfolio item created successfully."));
    }

    /// <summary>
    /// Updates an existing portfolio entry.
    /// </summary>
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

    /// <summary>
    /// Deletes a portfolio entry.
    /// </summary>
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

Run: `dotnet test tests/BarberSalon.IntegrationTests --filter "FullyQualifiedName~PortfolioIntegrationTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/BarberSalon.API/Controllers/PortfolioController.cs tests/BarberSalon.IntegrationTests/Portfolio/PortfolioIntegrationTests.cs
git commit -m "feat(api): add PortfolioController for before/after gallery entries"
```

---

### Task 8: BeautyProfileController (`GET /api/v1/beauty-profile/{id}`, `PUT /api/v1/beauty-profile/{id}`)

**Files:**
- Create: `src/BarberSalon.API/Controllers/BeautyProfileController.cs`
- Test: `tests/BarberSalon.IntegrationTests/BeautyProfile/BeautyProfileIntegrationTests.cs`

**Interfaces:**
- Consumes: `BeautyProfileService` in `BarberSalon.Application.BeautyProfile.Services`
- Produces: `GET /api/v1/beauty-profile/{customerId:guid}`, `PUT /api/v1/beauty-profile/{customerId:guid}`

- [ ] **Step 1: Write the failing integration test**

Create `tests/BarberSalon.IntegrationTests/BeautyProfile/BeautyProfileIntegrationTests.cs`:
```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BarberSalon.Application.BeautyProfile.DTOs;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Xunit;

namespace BarberSalon.IntegrationTests.BeautyProfile;

public class BeautyProfileIntegrationTests : IClassFixture<BarberSalonWebFactory>
{
    private const string BaseEndpoint = "/api/v1/beauty-profile";
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public BeautyProfileIntegrationTests(BarberSalonWebFactory factory)
    {
        _client = factory.GetClient();
    }

    [Fact]
    public async Task GetAndUpsertBeautyProfile_Succeeds()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var updateRequest = new UpdateBeautyProfileRequest(
            HairType: "wavy",
            CurrentHairColor: "قهوه‌ای تیره",
            Sensitivities: "حساسیت به رنگ‌های آمونیاک‌دار",
            Preferences: "مدل موی سایه‌روشن کلاسیک",
            Notes: "پوست سر خشک",
            SkinType: "dry",
            Allergies: "آمونیاک");

        // Act - Upsert
        var putResponse = await _client.PutAsJsonAsync($"{BaseEndpoint}/{customerId}", updateRequest);
        putResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var putEnvelope = await putResponse.Content.ReadFromJsonAsync<ApiResponseEnvelope<BeautyProfileDto>>(JsonOptions);
        putEnvelope!.Data!.CurrentHairColor.Should().Be("قهوه‌ای تیره");

        // Act - Get
        var getResponse = await _client.GetAsync($"{BaseEndpoint}/{customerId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getEnvelope = await getResponse.Content.ReadFromJsonAsync<ApiResponseEnvelope<BeautyProfileDto>>(JsonOptions);
        getEnvelope!.Data!.HairType.Should().Be("wavy");
        getEnvelope.Data.Allergies.Should().Be("آمونیاک");
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/BarberSalon.IntegrationTests --filter "FullyQualifiedName~BeautyProfileIntegrationTests"`
Expected: FAIL with HTTP 404 (NotFound).

- [ ] **Step 3: Write minimal implementation**

Create `src/BarberSalon.API/Controllers/BeautyProfileController.cs`:
```csharp
using BarberSalon.API.Common;
using BarberSalon.Application.BeautyProfile.DTOs;
using BarberSalon.Application.BeautyProfile.Services;
using BarberSalon.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace BarberSalon.API.Controllers;

/// <summary>
/// Manages customer beauty profile attributes (hair type, sensitivities, skin preferences).
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

    /// <summary>
    /// Returns a customer's beauty profile.
    /// </summary>
    [HttpGet("{customerId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<BeautyProfileDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByCustomerId(Guid customerId, CancellationToken cancellationToken = default)
    {
        var profile = await _beautyProfileService.GetByCustomerIdAsync(customerId, cancellationToken);
        return Ok(ApiResponse<BeautyProfileDto>.CreateSuccess(profile, "Beauty profile retrieved successfully."));
    }

    /// <summary>
    /// Creates or updates a customer's beauty profile.
    /// </summary>
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

Run: `dotnet test tests/BarberSalon.IntegrationTests --filter "FullyQualifiedName~BeautyProfileIntegrationTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/BarberSalon.API/Controllers/BeautyProfileController.cs tests/BarberSalon.IntegrationTests/BeautyProfile/BeautyProfileIntegrationTests.cs
git commit -m "feat(api): add BeautyProfileController for customer hair/skin preferences"
```

---

### Task 9: StaffPerformanceController (`GET /api/v1/staff-performance`)

**Files:**
- Create: `src/BarberSalon.API/Controllers/StaffPerformanceController.cs`
- Test: `tests/BarberSalon.IntegrationTests/StaffPerformance/StaffPerformanceIntegrationTests.cs`

**Interfaces:**
- Consumes: `StaffPerformanceService` in `BarberSalon.Application.StaffPerformance.Services`
- Produces: `GET /api/v1/staff-performance?from=&to=` returning `ApiResponse<List<StaffPerformanceDto>>`

- [ ] **Step 1: Write the failing integration test**

Create `tests/BarberSalon.IntegrationTests/StaffPerformance/StaffPerformanceIntegrationTests.cs`:
```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BarberSalon.Application.StaffPerformance.DTOs;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Xunit;

namespace BarberSalon.IntegrationTests.StaffPerformance;

public class StaffPerformanceIntegrationTests : IClassFixture<BarberSalonWebFactory>
{
    private const string BaseEndpoint = "/api/v1/staff-performance";
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public StaffPerformanceIntegrationTests(BarberSalonWebFactory factory)
    {
        _client = factory.GetClient();
    }

    [Fact]
    public async Task GetStaffPerformance_Returns200WithRankedMetrics()
    {
        // Act
        var response = await _client.GetAsync(BaseEndpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<List<StaffPerformanceDto>>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/BarberSalon.IntegrationTests --filter "FullyQualifiedName~StaffPerformanceIntegrationTests"`
Expected: FAIL with HTTP 404 (NotFound).

- [ ] **Step 3: Write minimal implementation**

Create `src/BarberSalon.API/Controllers/StaffPerformanceController.cs`:
```csharp
using BarberSalon.API.Common;
using BarberSalon.Application.StaffPerformance.DTOs;
using BarberSalon.Application.StaffPerformance.Services;
using Microsoft.AspNetCore.Mvc;

namespace BarberSalon.API.Controllers;

/// <summary>
/// Provides ranking and performance metrics for salon staff members.
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

    /// <summary>
    /// Returns aggregated and ranked performance metrics for staff over an optional date range.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<StaffPerformanceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStaffPerformance(
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

Run: `dotnet test tests/BarberSalon.IntegrationTests --filter "FullyQualifiedName~StaffPerformanceIntegrationTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/BarberSalon.API/Controllers/StaffPerformanceController.cs tests/BarberSalon.IntegrationTests/StaffPerformance/StaffPerformanceIntegrationTests.cs
git commit -m "feat(api): add StaffPerformanceController for staff metrics and rankings"
```

---

### Task 10: Full Backend Build & All Tests Green Verification

**Files:**
- Test: All solution tests across Domain, Application, and Integration suites.

- [ ] **Step 1: Execute complete test suite**

Run: `dotnet test tests/BarberSalon.IntegrationTests`
Expected: 100% PASS with 0 failures across all integration tests.

Run: `dotnet test tests/BarberSalon.Application.Tests`
Expected: 100% PASS with 0 failures.

Run: `dotnet test tests/BarberSalon.Domain.Tests`
Expected: 100% PASS with 0 failures.

- [ ] **Step 2: Commit final suite verification tag**

```bash
git commit --allow-empty -m "chore(test): verify all 9 missing API controllers pass integration tests"
```
