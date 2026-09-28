# Backend Analytics Endpoints Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Expose `computeRevenueForecast`, `computeAtRiskCustomers`, and `computeGapAnalysis` as real backend API endpoints so the frontend fetches computed analytics from the server instead of computing them client-side.

**Architecture:** Add three new methods to `DashboardService` that mirror the client-side logic in `dashboardAnalytics.ts`, then expose them via three new actions on `DashboardController`. Wire up three new RTK Query hooks in `dashboardApi.ts` and replace the three `useMemo` calls on `admin/dashboard/page.tsx` with hook calls.

**Tech Stack:** .NET 9 / C# (backend), Next.js 14 + RTK Query / TypeScript (frontend), xUnit + FluentAssertions + in-memory EF Core (backend tests), Vitest (frontend tests)

**Spec:** Gap analysis from previous session — three analytics functions currently live in `beauty-saloon-front-V2/src/lib/analytics/dashboardAnalytics.ts`; no corresponding backend endpoints exist.

## Global Constraints

- Backend envelope pattern: every response is `ApiResponse<T>` with `{ success, message, data, errors }` — never return raw data
- Backend routes must follow `api/v1/dashboard/<sub-resource>` pattern (controller already uses `[Route("api/v1/dashboard")]`)
- All backend services receive repositories via constructor injection; `DashboardService` already has `IAppointmentRepository`, `ICustomerRepository`, `IStaffRepository` (add this) available
- Frontend `dashboardApi.ts` must keep existing `ApiResponseEnvelope<T>` handling pattern for `transformResponse`
- Do NOT remove the client-side functions from `dashboardAnalytics.ts` — they may still be used by other pages or tests; just stop calling them from `dashboard/page.tsx`
- RTK Query hooks use `baseApi.injectEndpoints` — do not create a new `createApi` instance
- Backend date arithmetic must use `DateOnly` (not `DateTime`) for appointment filtering — see `DashboardService.cs:49`
- `IStaffRepository.GetAllAsync()` returns `List<StaffMember>` — `StaffMember` has `.IsActive` (bool) and `.FullName` (string) and `.Id` (Guid)

---

## Task 1: Add Analytics DTOs to Backend Application Layer

**Files:**
- Create: `src/BarberSalon.Application/Admin/DTOs/RevenueForecastDto.cs`
- Create: `src/BarberSalon.Application/Admin/DTOs/DailyRevenueBreakdownDto.cs`
- Create: `src/BarberSalon.Application/Admin/DTOs/AtRiskCustomerDto.cs`
- Create: `src/BarberSalon.Application/Admin/DTOs/TimeGapDto.cs`

**Interfaces:**
- Consumes: nothing
- Produces:
  - `RevenueForecastDto(string WeekLabel, decimal ConfirmedRevenue, decimal PendingRevenue, decimal TotalExpected, int ConfirmedCount, int PendingCount, int CancelledCount, List<DailyRevenueBreakdownDto> DailyBreakdown)`
  - `DailyRevenueBreakdownDto(string Date, string DayLabel, decimal Amount)`
  - `AtRiskCustomerDto(Guid CustomerId, string CustomerName, string Phone, string? LastAppointmentDate, int DaysSinceVisit, int AppointmentsCount, string RiskLevel, string SuggestedAction)`
  - `TimeGapDto(string Id, string Date, string DayLabel, string Time, Guid StaffId, string StaffName, int DurationMinutes)`

- [ ] **Step 1: Write the DTO files**

`src/BarberSalon.Application/Admin/DTOs/DailyRevenueBreakdownDto.cs`:
```csharp
namespace BarberSalon.Application.Admin.DTOs;

public sealed record DailyRevenueBreakdownDto(string Date, string DayLabel, decimal Amount);
```

`src/BarberSalon.Application/Admin/DTOs/RevenueForecastDto.cs`:
```csharp
namespace BarberSalon.Application.Admin.DTOs;

public sealed record RevenueForecastDto(
    string WeekLabel,
    decimal ConfirmedRevenue,
    decimal PendingRevenue,
    decimal TotalExpected,
    int ConfirmedCount,
    int PendingCount,
    int CancelledCount,
    List<DailyRevenueBreakdownDto> DailyBreakdown);
```

`src/BarberSalon.Application/Admin/DTOs/AtRiskCustomerDto.cs`:
```csharp
namespace BarberSalon.Application.Admin.DTOs;

public sealed record AtRiskCustomerDto(
    Guid CustomerId,
    string CustomerName,
    string Phone,
    string? LastAppointmentDate,
    int DaysSinceVisit,
    int AppointmentsCount,
    string RiskLevel,
    string SuggestedAction);
```

`src/BarberSalon.Application/Admin/DTOs/TimeGapDto.cs`:
```csharp
namespace BarberSalon.Application.Admin.DTOs;

public sealed record TimeGapDto(
    string Id,
    string Date,
    string DayLabel,
    string Time,
    Guid StaffId,
    string StaffName,
    int DurationMinutes);
```

- [ ] **Step 2: Build backend to verify DTOs compile**

```bash
cd e:\barber\beauty-saloon-api
dotnet build src/BarberSalon.Application/BarberSalon.Application.csproj
```
Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Commit**

```bash
git add src/BarberSalon.Application/Admin/DTOs/RevenueForecastDto.cs \
        src/BarberSalon.Application/Admin/DTOs/DailyRevenueBreakdownDto.cs \
        src/BarberSalon.Application/Admin/DTOs/AtRiskCustomerDto.cs \
        src/BarberSalon.Application/Admin/DTOs/TimeGapDto.cs
git commit -m "feat(analytics): add analytics DTO types to Application layer"
```

---

## Task 2: Add `IStaffRepository` to `DashboardService` and Implement Three Analytics Methods

**Files:**
- Modify: `src/BarberSalon.Application/Admin/Services/DashboardService.cs` (whole file)

**Interfaces:**
- Consumes: `RevenueForecastDto`, `DailyRevenueBreakdownDto`, `AtRiskCustomerDto`, `TimeGapDto` (from Task 1)
- Consumes: `IStaffRepository.GetAllAsync(CancellationToken)` → `List<StaffMember>`; `StaffMember.IsActive` (bool), `StaffMember.FullName` (string), `StaffMember.Id` (Guid)
- Consumes: `IAppointmentRepository.GetAllAsync(ct)` → `List<Appointment>`; `Appointment.TimeSlot.Date` (DateOnly), `Appointment.Status` (AppointmentStatus enum: Pending, Confirmed, Completed, Cancelled, NoShow), `Appointment.Price` (decimal), `Appointment.StaffId` (Guid), `Appointment.TimeSlot.StartTime` (TimeOnly)
- Consumes: `ICustomerRepository.GetAllAsync(ct)` → `List<Customer>`; `Customer.Id` (Guid), `Customer.FullName` (string), `Customer.PhoneNumber` (string), `Customer.LastAppointmentDate` (DateOnly?), `Customer.TotalAppointments` (int), `Customer.IsActive` (bool)
- Produces:
  - `GetRevenueForecastAsync(CancellationToken) → Task<RevenueForecastDto>`
  - `GetAtRiskCustomersAsync(CancellationToken) → Task<List<AtRiskCustomerDto>>`
  - `GetGapAnalysisAsync(int maxGaps, CancellationToken) → Task<List<TimeGapDto>>`

> **Note:** Check actual `Customer` and `StaffMember` domain entity property names before coding. If `Customer.PhoneNumber` is actually `PhoneNumber` or `Phone`, use what the entity has. Use `get_code_snippet` or read the entity source to verify.

- [ ] **Step 1: Read the actual domain entity property names**

Read `src/BarberSalon.Domain/Customers/Entities/Customer.cs` and `src/BarberSalon.Domain/Staff/Entities/StaffMember.cs` before writing any code. Note the exact property names for: phone number, last appointment date, total appointments count, IsActive.

- [ ] **Step 2: Write the failing unit test**

Create `tests/BarberSalon.UnitTests/Admin/DashboardAnalyticsServiceTests.cs`:

```csharp
using BarberSalon.Application.Admin.Services;
using BarberSalon.Application.Booking.Interfaces;
using BarberSalon.Application.Common.Interfaces;
using BarberSalon.Application.Customers.Interfaces;
using BarberSalon.Application.Staff.Interfaces;
using BarberSalon.Domain.Booking.Entities;
using BarberSalon.Domain.Booking.Enums;
using BarberSalon.Domain.Booking.ValueObjects;
using BarberSalon.Domain.Customers.Entities;
using BarberSalon.Domain.Staff.Entities;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace BarberSalon.UnitTests.Admin;

public class DashboardAnalyticsServiceTests
{
    private readonly IAppointmentRepository _appointmentRepo = Substitute.For<IAppointmentRepository>();
    private readonly ICustomerRepository _customerRepo = Substitute.For<ICustomerRepository>();
    private readonly IStaffRepository _staffRepo = Substitute.For<IStaffRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();

    private DashboardService CreateService() =>
        new(_appointmentRepo, _clock, _customerRepo, null, _staffRepo);

    [Fact]
    public async Task GetRevenueForecastAsync_WithConfirmedAppointmentThisWeek_ReturnsCorrectTotals()
    {
        // Arrange — use the real today so "this week" window covers today
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        _clock.UtcNow.Returns(DateTime.UtcNow);

        var user = Guid.NewGuid();
        var staff = Guid.NewGuid();
        var svc = Guid.NewGuid();
        var slot = TimeSlot.Create(today, new TimeOnly(10, 0), new TimeOnly(10, 30));
        var apt = Appointment.Create(user, staff, svc, slot, 300_000m);
        apt.Confirm();

        _appointmentRepo.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns([apt]);

        // Act
        var result = await CreateService().GetRevenueForecastAsync(CancellationToken.None);

        // Assert
        result.ConfirmedRevenue.Should().Be(300_000m);
        result.PendingRevenue.Should().Be(0m);
        result.TotalExpected.Should().Be(300_000m);
        result.ConfirmedCount.Should().Be(1);
        result.DailyBreakdown.Should().HaveCount(7);
    }

    [Fact]
    public async Task GetAtRiskCustomersAsync_CustomerInactiveOver25Days_IsHighRisk()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        _clock.UtcNow.Returns(DateTime.UtcNow);

        // Use Customer.Create() factory — read entity source for exact signature
        // The customer's last appointment is 30 days ago
        var lastAppt = today.AddDays(-30);
        // Build via reflection or factory — fill in after reading entity source
        // Placeholder: assume Customer has a Create factory accepting (fullName, phone)
        // and a SetLastAppointment or property setter

        _customerRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns([]);
        _appointmentRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns([]);

        // Act
        var result = await CreateService().GetAtRiskCustomersAsync(CancellationToken.None);

        // Assert — empty when no customers
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetGapAnalysisAsync_ActiveStaffWithNoAppointments_ReturnsGaps()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        _clock.UtcNow.Returns(DateTime.UtcNow);

        // Build a StaffMember — use StaffMember.Create factory (read entity source)
        // StaffMember.Create("آرایشگر", "slug", "09120000001", "bio", "role", 5)
        _staffRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns([]);
        _appointmentRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns([]);

        // Act
        var result = await CreateService().GetGapAnalysisAsync(12, CancellationToken.None);

        // Assert — no staff → no gaps
        result.Should().BeEmpty();
    }
}
```

- [ ] **Step 3: Run the test to confirm it fails (method not yet implemented)**

```bash
cd e:\barber\beauty-saloon-api
dotnet test tests/BarberSalon.UnitTests --filter "DashboardAnalyticsServiceTests" -v minimal
```
Expected: Compilation error — `DashboardService` has no `GetRevenueForecastAsync`, `GetAtRiskCustomersAsync`, `GetGapAnalysisAsync` methods, and constructor doesn't accept `IStaffRepository`.

- [ ] **Step 4: Implement the three analytics methods in `DashboardService`**

Add `IStaffRepository` as optional ctor param (after `ISalonServiceRepository`). Add the three methods below. The week window is today ± 6 days (rolling 7-day window starting from `today.AddDays(-6)` to `today`). Time slots are 09:00–17:00 in 30-minute increments.

Add to `DashboardService.cs` constructor:
```csharp
private readonly IStaffRepository? _staffRepository;

// update ctor signature:
public DashboardService(
    IAppointmentRepository appointmentRepository,
    IClock clock,
    ICustomerRepository? customerRepository = null,
    ISalonServiceRepository? salonServiceRepository = null,
    IStaffRepository? staffRepository = null)
{
    _appointmentRepository = appointmentRepository;
    _clock = clock;
    _customerRepository = customerRepository;
    _salonServiceRepository = salonServiceRepository;
    _staffRepository = staffRepository;
}
```

Add the three methods (translate the TypeScript logic from `dashboardAnalytics.ts`):

```csharp
public async Task<RevenueForecastDto> GetRevenueForecastAsync(CancellationToken cancellationToken = default)
{
    cancellationToken.ThrowIfCancellationRequested();
    var today = DateOnly.FromDateTime(_clock.UtcNow);
    var weekStart = today.AddDays(-6);

    var allAppointments = await _appointmentRepository.GetAllAsync(cancellationToken);
    var weekAppts = allAppointments
        .Where(a => a.TimeSlot.Date >= weekStart && a.TimeSlot.Date <= today)
        .ToList();

    decimal confirmedRevenue = 0, pendingRevenue = 0;
    int confirmedCount = 0, pendingCount = 0, cancelledCount = 0;

    foreach (var a in weekAppts)
    {
        switch (a.Status)
        {
            case AppointmentStatus.Confirmed:
            case AppointmentStatus.Completed:
                confirmedRevenue += a.Price;
                confirmedCount++;
                break;
            case AppointmentStatus.Pending:
                pendingRevenue += a.Price;
                pendingCount++;
                break;
            case AppointmentStatus.Cancelled:
            case AppointmentStatus.NoShow:
                cancelledCount++;
                break;
        }
    }

    var dailyBreakdown = Enumerable.Range(0, 7)
        .Select(i => weekStart.AddDays(i))
        .Select(date => new DailyRevenueBreakdownDto(
            Date: date.ToString("yyyy-MM-dd"),
            DayLabel: date.DayOfWeek.ToString(),
            Amount: weekAppts
                .Where(a => a.TimeSlot.Date == date &&
                    a.Status is AppointmentStatus.Confirmed or AppointmentStatus.Completed or AppointmentStatus.Pending)
                .Sum(a => a.Price)))
        .ToList();

    return new RevenueForecastDto(
        WeekLabel: $"{weekStart:yyyy-MM-dd} تا {today:yyyy-MM-dd}",
        ConfirmedRevenue: confirmedRevenue,
        PendingRevenue: pendingRevenue,
        TotalExpected: confirmedRevenue + pendingRevenue,
        ConfirmedCount: confirmedCount,
        PendingCount: pendingCount,
        CancelledCount: cancelledCount,
        DailyBreakdown: dailyBreakdown);
}

public async Task<List<AtRiskCustomerDto>> GetAtRiskCustomersAsync(CancellationToken cancellationToken = default)
{
    cancellationToken.ThrowIfCancellationRequested();
    if (_customerRepository is null) return [];

    var today = DateOnly.FromDateTime(_clock.UtcNow);
    var customers = await _customerRepository.GetAllAsync(cancellationToken);
    var appointments = await _appointmentRepository.GetAllAsync(cancellationToken);

    var apptCountByCustomer = appointments
        .GroupBy(a => a.CustomerId)
        .ToDictionary(g => g.Key, g => g.Count());

    var atRisk = new List<AtRiskCustomerDto>();

    foreach (var c in customers)
    {
        // Use actual property name from entity (check: LastAppointmentDate or similar)
        // Adjust the property references below after reading Customer entity source
        DateOnly? lastAppt = null; // replace with: c.LastAppointmentDate
        int daysSince = lastAppt.HasValue
            ? today.DayNumber - lastAppt.Value.DayNumber
            : 999;

        string? riskLevel = c.IsActive switch
        {
            false => "high",
            _ when daysSince >= 25 => "high",
            _ when daysSince >= 14 => "medium",
            _ when daysSince >= 7 => "low",
            _ => null
        };

        if (riskLevel is null) continue;

        var apptCount = apptCountByCustomer.GetValueOrDefault(c.Id, 0);
        string suggested = (riskLevel, apptCount) switch
        {
            ("high", > 0) => "ارسال پیامک تخفیف بازگشت — بیش از ۳ هفته غایب",
            ("high", _) => "تماس خوش‌آمدگویی — مشتری جدید بدون مراجعه",
            ("medium", _) => "یادآوری نوبت + پیشنهاد سرویس مکمل",
            _ => $"پیگیری ملایم — {daysSince} روز از آخرین مراجعه"
        };

        atRisk.Add(new AtRiskCustomerDto(
            CustomerId: c.Id,
            CustomerName: c.FullName,   // adjust if property name differs
            Phone: c.PhoneNumber,       // adjust if property name differs
            LastAppointmentDate: lastAppt?.ToString("yyyy-MM-dd"),
            DaysSinceVisit: daysSince,
            AppointmentsCount: apptCount,
            RiskLevel: riskLevel,
            SuggestedAction: suggested));
    }

    return atRisk
        .OrderBy(x => x.RiskLevel switch { "high" => 0, "medium" => 1, _ => 2 })
        .ThenByDescending(x => x.DaysSinceVisit)
        .ToList();
}

public async Task<List<TimeGapDto>> GetGapAnalysisAsync(int maxGaps = 12, CancellationToken cancellationToken = default)
{
    cancellationToken.ThrowIfCancellationRequested();
    if (_staffRepository is null) return [];

    var today = DateOnly.FromDateTime(_clock.UtcNow);
    var weekStart = today.AddDays(-6);

    var allStaff = await _staffRepository.GetAllAsync(cancellationToken);
    var activeStaff = allStaff.Where(s => s.IsActive).ToList();

    var allAppointments = await _appointmentRepository.GetAllAsync(cancellationToken);

    // 30-min slots from 09:00 to 17:00
    var allSlots = Enumerable.Range(0, 17)
        .Select(i => new TimeOnly(9 + i / 2, (i % 2) * 30))
        .Where(t => t < new TimeOnly(17, 0))
        .ToList();

    var weekDates = Enumerable.Range(0, 7)
        .Select(i => weekStart.AddDays(i))
        .ToList();

    var gaps = new List<TimeGapDto>();

    foreach (var date in weekDates)
    {
        foreach (var member in activeStaff)
        {
            var bookedTimes = allAppointments
                .Where(a => a.TimeSlot.Date == date &&
                            a.StaffId == member.Id &&
                            a.Status != AppointmentStatus.Cancelled)
                .Select(a => a.TimeSlot.StartTime)
                .ToHashSet();

            foreach (var slot in allSlots)
            {
                if (!bookedTimes.Contains(slot))
                {
                    gaps.Add(new TimeGapDto(
                        Id: $"gap_{date:yyyy-MM-dd}_{member.Id}_{slot:HH\\:mm}",
                        Date: date.ToString("yyyy-MM-dd"),
                        DayLabel: date.DayOfWeek.ToString(),
                        Time: slot.ToString("HH:mm"),
                        StaffId: member.Id,
                        StaffName: member.FullName,  // adjust if name differs
                        DurationMinutes: 30));
                }
            }
        }
    }

    return gaps
        .OrderBy(g => g.Date)
        .ThenBy(g => g.Time)
        .Take(maxGaps)
        .ToList();
}
```

- [ ] **Step 5: Fix test stubs now that entity factories are known, run the unit tests**

```bash
cd e:\barber\beauty-saloon-api
dotnet test tests/BarberSalon.UnitTests --filter "DashboardAnalyticsServiceTests" -v minimal
```
Expected: All 3 tests pass (green).

- [ ] **Step 6: Run full unit test suite to confirm no regressions**

```bash
dotnet test tests/BarberSalon.UnitTests -v minimal
```
Expected: All tests pass.

- [ ] **Step 7: Commit**

```bash
git add src/BarberSalon.Application/Admin/Services/DashboardService.cs \
        tests/BarberSalon.UnitTests/Admin/DashboardAnalyticsServiceTests.cs
git commit -m "feat(analytics): implement revenue forecast, at-risk customers, gap analysis in DashboardService"
```

---

## Task 3: Register `IStaffRepository` in `DashboardService` DI and Expose Three Controller Endpoints

**Files:**
- Modify: `src/BarberSalon.API/DependencyInjection.cs`
- Modify: `src/BarberSalon.API/Controllers/DashboardController.cs`

**Interfaces:**
- Consumes: `DashboardService.GetRevenueForecastAsync(ct)` → `RevenueForecastDto`
- Consumes: `DashboardService.GetAtRiskCustomersAsync(ct)` → `List<AtRiskCustomerDto>`
- Consumes: `DashboardService.GetGapAnalysisAsync(int maxGaps, ct)` → `List<TimeGapDto>`
- Produces: HTTP endpoints:
  - `GET /api/v1/dashboard/revenue-forecast` → `ApiResponse<RevenueForecastDto>`
  - `GET /api/v1/dashboard/at-risk-customers` → `ApiResponse<List<AtRiskCustomerDto>>`
  - `GET /api/v1/dashboard/gap-analysis?maxGaps=12` → `ApiResponse<List<TimeGapDto>>`

- [ ] **Step 1: Write the failing integration test**

Create `tests/BarberSalon.IntegrationTests/Admin/AnalyticsIntegrationTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BarberSalon.Application.Admin.DTOs;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Xunit;

namespace BarberSalon.IntegrationTests.Admin;

public class AnalyticsIntegrationTests : IClassFixture<BarberSalonWebFactory>
{
    private readonly HttpClient _client;

    public AnalyticsIntegrationTests(BarberSalonWebFactory factory)
        => _client = factory.GetClient();

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task GetRevenueForecast_Returns200WithEnvelope()
    {
        var response = await _client.GetAsync("/api/v1/dashboard/revenue-forecast");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content
            .ReadFromJsonAsync<ApiEnvelope<RevenueForecastDto>>(JsonOptions);
        envelope!.Success.Should().BeTrue();
        envelope.Data.DailyBreakdown.Should().HaveCount(7);
        envelope.Data.TotalExpected.Should().Be(envelope.Data.ConfirmedRevenue + envelope.Data.PendingRevenue);
    }

    [Fact]
    public async Task GetAtRiskCustomers_Returns200WithList()
    {
        var response = await _client.GetAsync("/api/v1/dashboard/at-risk-customers");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content
            .ReadFromJsonAsync<ApiEnvelope<List<AtRiskCustomerDto>>>(JsonOptions);
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetGapAnalysis_Returns200WithList()
    {
        var response = await _client.GetAsync("/api/v1/dashboard/gap-analysis?maxGaps=5");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content
            .ReadFromJsonAsync<ApiEnvelope<List<TimeGapDto>>>(JsonOptions);
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
        envelope.Data.Should().HaveCountLessOrEqualTo(5);
    }

    private sealed record ApiEnvelope<T>(T Data, bool Success, string Message);
}
```

- [ ] **Step 2: Run to confirm it fails (404s on nonexistent endpoints)**

```bash
cd e:\barber\beauty-saloon-api
dotnet test tests/BarberSalon.IntegrationTests --filter "AnalyticsIntegrationTests" -v minimal
```
Expected: Tests fail with HTTP 404 or route-not-found.

- [ ] **Step 3: Register `IStaffRepository` injection in `DashboardService`**

In `src/BarberSalon.API/DependencyInjection.cs`, find where `DashboardService` is registered (as `Scoped`) and ensure it resolves `IStaffRepository`. Since `DashboardService` uses optional constructor injection, DI will inject it automatically as long as `IStaffRepository` is registered — it already is in `src/BarberSalon.Infrastructure/DependencyInjection.cs` (line 58). No change needed here unless `DashboardService` is registered without resolving `IStaffRepository`. Verify by searching `DashboardService` registration in `src/BarberSalon.API/DependencyInjection.cs`.

- [ ] **Step 4: Add three HTTP actions to `DashboardController`**

Append to `src/BarberSalon.API/Controllers/DashboardController.cs` (inside class, before closing brace):

```csharp
/// <summary>Returns revenue forecast for the current rolling 7-day window.</summary>
[HttpGet("revenue-forecast")]
[ProducesResponseType(typeof(ApiResponse<RevenueForecastDto>), StatusCodes.Status200OK)]
public async Task<IActionResult> GetRevenueForecast(CancellationToken cancellationToken = default)
{
    var forecast = await _dashboardService.GetRevenueForecastAsync(cancellationToken);
    return Ok(ApiResponse<RevenueForecastDto>.CreateSuccess(forecast, "Revenue forecast retrieved successfully."));
}

/// <summary>Returns customers at risk of churning.</summary>
[HttpGet("at-risk-customers")]
[ProducesResponseType(typeof(ApiResponse<List<AtRiskCustomerDto>>), StatusCodes.Status200OK)]
public async Task<IActionResult> GetAtRiskCustomers(CancellationToken cancellationToken = default)
{
    var atRisk = await _dashboardService.GetAtRiskCustomersAsync(cancellationToken);
    return Ok(ApiResponse<List<AtRiskCustomerDto>>.CreateSuccess(atRisk, "At-risk customers retrieved successfully."));
}

/// <summary>Returns schedule gaps (unfilled slots) for the current week.</summary>
[HttpGet("gap-analysis")]
[ProducesResponseType(typeof(ApiResponse<List<TimeGapDto>>), StatusCodes.Status200OK)]
public async Task<IActionResult> GetGapAnalysis([FromQuery] int maxGaps = 12, CancellationToken cancellationToken = default)
{
    var gaps = await _dashboardService.GetGapAnalysisAsync(maxGaps, cancellationToken);
    return Ok(ApiResponse<List<TimeGapDto>>.CreateSuccess(gaps, "Gap analysis retrieved successfully."));
}
```

Also add the missing `using` statement for the new DTOs at the top of `DashboardController.cs`:
```csharp
using BarberSalon.Application.Admin.DTOs;
```

- [ ] **Step 5: Run the integration tests**

```bash
dotnet test tests/BarberSalon.IntegrationTests --filter "AnalyticsIntegrationTests" -v minimal
```
Expected: All 3 tests pass (green).

- [ ] **Step 6: Run the full integration test suite**

```bash
dotnet test tests/BarberSalon.IntegrationTests -v minimal
```
Expected: All existing tests still pass.

- [ ] **Step 7: Commit**

```bash
git add src/BarberSalon.API/Controllers/DashboardController.cs \
        tests/BarberSalon.IntegrationTests/Admin/AnalyticsIntegrationTests.cs
git commit -m "feat(analytics): expose revenue-forecast, at-risk-customers, gap-analysis endpoints"
```

---

## Task 4: Wire Frontend RTK Query Hooks and Replace `useMemo` on Dashboard Page

**Files:**
- Modify: `beauty-saloon-front-V2/src/store/api/dashboardApi.ts`
- Modify: `beauty-saloon-front-V2/src/app/admin/dashboard/page.tsx`

**Interfaces:**
- Consumes backend: `GET /api/v1/dashboard/revenue-forecast` → envelope `{ success, data: RevenueForecastDto }`
- Consumes backend: `GET /api/v1/dashboard/at-risk-customers` → envelope `{ success, data: AtRiskCustomerDto[] }`
- Consumes backend: `GET /api/v1/dashboard/gap-analysis?maxGaps=12` → envelope `{ success, data: TimeGapDto[] }`
- Frontend types are already defined in `@/lib/data/types.ts`: `RevenueForecast`, `AtRiskCustomer`, `TimeGap`
- Produces: hooks `useGetRevenueForecastQuery`, `useGetAtRiskCustomersQuery`, `useGetGapAnalysisQuery` exported from `dashboardApi`

> **Note:** The backend DTO uses camelCase keys (`confirmedRevenue`, `dailyBreakdown`, etc.) — verify the exact JSON field names returned by `dotnet run` or integration tests, and map to frontend types accordingly.

- [ ] **Step 1: Write the failing frontend test**

Create `beauty-saloon-front-V2/src/store/api/__tests__/dashboardAnalytics.test.ts`:

```typescript
// Verifies that the RTK Query hooks are exported from dashboardApi
import { dashboardApi } from "../dashboardApi";

describe("dashboardApi analytics endpoints", () => {
  it("exports useGetRevenueForecastQuery", () => {
    expect(dashboardApi.endpoints.getRevenueForecast).toBeDefined();
  });

  it("exports useGetAtRiskCustomersQuery", () => {
    expect(dashboardApi.endpoints.getAtRiskCustomers).toBeDefined();
  });

  it("exports useGetGapAnalysisQuery", () => {
    expect(dashboardApi.endpoints.getGapAnalysis).toBeDefined();
  });
});
```

- [ ] **Step 2: Run to confirm it fails**

```bash
cd e:\barber\beauty-saloon-front-V2
npx vitest run src/store/api/__tests__/dashboardAnalytics.test.ts
```
Expected: All 3 tests fail — endpoints don't exist yet.

- [ ] **Step 3: Add three new backend DTO types and three endpoints to `dashboardApi.ts`**

Add the following type definitions near the top of `dashboardApi.ts` (after existing interfaces):

```typescript
// --- Analytics DTOs (mirrors backend) ---
export interface BackendDailyRevenueBreakdownDto {
  date: string;
  dayLabel: string;
  amount: number;
}

export interface BackendRevenueForecastDto {
  weekLabel: string;
  confirmedRevenue: number;
  pendingRevenue: number;
  totalExpected: number;
  confirmedCount: number;
  pendingCount: number;
  cancelledCount: number;
  dailyBreakdown: BackendDailyRevenueBreakdownDto[];
}

export interface BackendAtRiskCustomerDto {
  customerId: string;
  customerName: string;
  phone: string;
  lastAppointmentDate: string | null;
  daysSinceVisit: number;
  appointmentsCount: number;
  riskLevel: "high" | "medium" | "low";
  suggestedAction: string;
}

export interface BackendTimeGapDto {
  id: string;
  date: string;
  dayLabel: string;
  time: string;
  staffId: string;
  staffName: string;
  durationMinutes: number;
}
```

Then inside the `dashboardApi = baseApi.injectEndpoints({ endpoints: (builder) => ({ ... }) })` block, add three new endpoints:

```typescript
getRevenueForecast: builder.query<RevenueForecast, void>({
  query: () => "/api/v1/dashboard/revenue-forecast",
  transformResponse: (response: ApiResponseEnvelope<BackendRevenueForecastDto> | BackendRevenueForecastDto) => {
    const dto = response && "data" in response && (response as ApiResponseEnvelope<BackendRevenueForecastDto>).data
      ? (response as ApiResponseEnvelope<BackendRevenueForecastDto>).data
      : (response as BackendRevenueForecastDto);
    return {
      weekLabel: dto.weekLabel,
      confirmedRevenue: dto.confirmedRevenue,
      pendingRevenue: dto.pendingRevenue,
      totalExpected: dto.totalExpected,
      confirmedCount: dto.confirmedCount,
      pendingCount: dto.pendingCount,
      cancelledCount: dto.cancelledCount,
      dailyBreakdown: dto.dailyBreakdown.map(d => ({
        date: d.date,
        dayLabel: d.dayLabel,
        amount: d.amount,
      })),
    };
  },
}),

getAtRiskCustomers: builder.query<AtRiskCustomer[], void>({
  query: () => "/api/v1/dashboard/at-risk-customers",
  transformResponse: (response: ApiResponseEnvelope<BackendAtRiskCustomerDto[]> | BackendAtRiskCustomerDto[]) => {
    const arr = response && "data" in response && Array.isArray((response as ApiResponseEnvelope<BackendAtRiskCustomerDto[]>).data)
      ? (response as ApiResponseEnvelope<BackendAtRiskCustomerDto[]>).data
      : (response as BackendAtRiskCustomerDto[]);
    return arr.map(d => ({
      customerId: d.customerId,
      customerName: d.customerName,
      phone: d.phone,
      lastAppointment: d.lastAppointmentDate,
      daysSinceVisit: d.daysSinceVisit,
      appointmentsCount: d.appointmentsCount,
      riskLevel: d.riskLevel,
      suggestedAction: d.suggestedAction,
    }));
  },
}),

getGapAnalysis: builder.query<TimeGap[], number | void>({
  query: (maxGaps = 12) => `/api/v1/dashboard/gap-analysis?maxGaps=${maxGaps}`,
  transformResponse: (response: ApiResponseEnvelope<BackendTimeGapDto[]> | BackendTimeGapDto[]) => {
    const arr = response && "data" in response && Array.isArray((response as ApiResponseEnvelope<BackendTimeGapDto[]>).data)
      ? (response as ApiResponseEnvelope<BackendTimeGapDto[]>).data
      : (response as BackendTimeGapDto[]);
    return arr.map(d => ({
      id: d.id,
      date: d.date,
      dayLabel: d.dayLabel,
      time: d.time,
      staffId: d.staffId,
      staffName: d.staffName,
      durationMinutes: d.durationMinutes,
    }));
  },
}),
```

Add the necessary type imports at the top of `dashboardApi.ts`:
```typescript
import type { DashboardStats, TimeSlot, RevenueForecast, AtRiskCustomer, TimeGap } from "@/lib/data/types";
```

- [ ] **Step 4: Run the frontend test**

```bash
npx vitest run src/store/api/__tests__/dashboardAnalytics.test.ts
```
Expected: All 3 tests pass.

- [ ] **Step 5: Replace `useMemo` calls on the dashboard page**

In `beauty-saloon-front-V2/src/app/admin/dashboard/page.tsx`:

Replace the three `useMemo` blocks (lines 75-88) with hook calls. Remove the import of `computeRevenueForecast`, `computeAtRiskCustomers`, `computeGapAnalysis` from `dashboardAnalytics.ts`. Add the new hook imports from `dashboardApi`.

Before (lines 75-88):
```typescript
const revenueForecast = useMemo(
  () => computeRevenueForecast(effectiveAppointments),
  [effectiveAppointments]
);

const atRiskCustomers = useMemo(
  () => computeAtRiskCustomers(effectiveCustomers, ADMIN_TODAY_DATE),
  [effectiveCustomers]
);

const gaps = useMemo(
  () => computeGapAnalysis(effectiveAppointments, effectiveStaff),
  [effectiveAppointments, effectiveStaff]
);
```

After:
```typescript
const { data: revenueForecast } = useGetRevenueForecastQuery();
const { data: atRiskCustomers = [] } = useGetAtRiskCustomersQuery();
const { data: gaps = [] } = useGetGapAnalysisQuery(12);
```

Add hook imports at top of the file (alongside other RTK imports):
```typescript
import {
  useGetRevenueForecastQuery,
  useGetAtRiskCustomersQuery,
  useGetGapAnalysisQuery,
} from "@/store/api/dashboardApi";
```

Remove the now-unused import of `computeRevenueForecast`, `computeAtRiskCustomers`, `computeGapAnalysis` from the file header. Keep the import of `ADMIN_TODAY_DATE` only if it's used elsewhere in the file.

- [ ] **Step 6: Verify TypeScript compiles**

```bash
cd e:\barber\beauty-saloon-front-V2
npx tsc --noEmit
```
Expected: 0 errors.

- [ ] **Step 7: Run all frontend tests**

```bash
npx vitest run
```
Expected: All tests pass.

- [ ] **Step 8: Commit**

```bash
git add src/store/api/dashboardApi.ts \
        src/app/admin/dashboard/page.tsx \
        src/store/api/__tests__/dashboardAnalytics.test.ts
git commit -m "feat(analytics): wire RTK Query analytics hooks to real backend endpoints"
```
