# BarberSalon Backend — Agent Instruction Manual

> **Single source of truth** for AI agents working on **BarberSalon.API** (.NET 10).
> Read this file **completely** before writing any code, creating any file, or running any command.
> **Domain vocabulary:** Read `CONTEXT.md` for precise definitions of all business terms before naming anything.
> **Pattern reference:** Check `docs/examples/SalonService-complete.md` when implementing any module.

---

## Architecture Decision Log

All decisions below were finalized on 2026-08-29 and are **locked**.
Do NOT change any of them without an explicit user request.

| Topic | Decision | Reason |
|-------|----------|--------|
| Framework | **.NET 10 LTS** | Supported until Nov 2028; stable ecosystem |
| Internal architecture | **Clean Architecture + DDD Lite** | Simple, testable, evolvable |
| Layer organisation | **Layer-First** (not Module-First) | Solo dev; easier navigation; less boilerplate |
| Domain layer name | **`BarberSalon.Domain`** (not Core) | Semantically correct for DDD |
| Repository Interface location | **`Application/<Module>/Interfaces/`** | Use-cases own their data contracts |
| ORM | **EF Core Code First** | Standard .NET; migrations live in Infrastructure |
| Database | **PostgreSQL** | UUID/JSON flexibility; Jalali-aware |
| API style | **REST** | CRUD-heavy app; free OpenAPI/Swagger |
| Real-time | **SignalR** (only where needed) | Calendar sync; waitlist notifications |
| Auth | **Custom JWT + Phone OTP** (no ASP.NET Identity) | OTP-based flow; Identity is email/password-first |
| CQRS | ❌ No | Simple business logic; overhead with no benefit |
| MediatR / Command handlers | ❌ No | Plain Services only — no Command/Query per method |
| Microservices | ❌ No | Solo dev; current scale does not justify it |
| Docker | ❌ No | Deploy directly to VPS or Azure App Service |
| Message queue | ❌ Not yet | SMS via simple IHostedService; add later if needed |
| Redis | ❌ Not yet | OTP stored in DB table; add later if needed |
| Event Sourcing | ❌ No | |
| ASP.NET Identity | ❌ No | Custom JWT is sufficient |

---

## §1 Solution Structure

```
e:\barber\
├── beauty-saloon-front-V2/          ← Next.js frontend (separate project)
└── beauty-saloon-api/               ← This project
    ├── BarberSalon.sln
    ├── src/
    │   ├── BarberSalon.Domain/
    │   ├── BarberSalon.Application/
    │   ├── BarberSalon.Infrastructure/
    │   └── BarberSalon.API/
    ├── tests/
    │   ├── BarberSalon.Domain.Tests/
    │   └── BarberSalon.Application.Tests/
    └── docs/
        └── examples/
            └── SalonService-complete.md   ← Reference implementation
```

---

## §2 Folder Structure

### `BarberSalon.Domain`

> Contains ONLY: Entities, Value Objects, Enums, Domain Exceptions.
> Zero external dependencies. No EF Core. No interfaces.

```
BarberSalon.Domain/
├── Common/
│   ├── BaseEntity.cs           ← Id (Guid), CreatedAt, UpdatedAt, Touch()
│   └── ValueObject.cs          ← Base class for value objects
├── Auth/
│   ├── Entities/
│   │   └── User.cs             ← Id, PhoneNumber, Role, RefreshToken, RefreshTokenExpiry
│   └── Enums/
│       └── UserRole.cs         ← Customer, Staff, Admin
├── Booking/
│   ├── Entities/
│   │   └── Appointment.cs      ← Id, CustomerId, StaffId, ServiceId, TimeSlot, Status
│   ├── ValueObjects/
│   │   └── TimeSlot.cs         ← Date, StartTime, EndTime (immutable)
│   └── Enums/
│       └── AppointmentStatus.cs ← Pending, Confirmed, Completed, Cancelled, NoShow
├── Customers/
│   └── Entities/
│       └── Customer.cs         ← Id, FullName, PhoneNumber, UserId
├── Staff/
│   └── Entities/
│       └── StaffMember.cs      ← Id, FullName, PhoneNumber, UserId, IsActive
├── SalonServices/
│   └── Entities/
│       └── SalonService.cs     ← Id, Name, Duration, Price, Category, IsActive
├── Loyalty/
│   └── Entities/
│       ├── LoyaltyAccount.cs   ← Id, CustomerId, Points, Tier
│       └── Referral.cs         ← Id, ReferrerId, ReferredId, IsRedeemed
├── Waitlist/
│   └── Entities/
│       └── WaitlistEntry.cs    ← Id, CustomerId, ServiceId, StaffId, RequestedDate
└── Reviews/
    └── Entities/
        └── Review.cs           ← Id, CustomerId, AppointmentId, Rating, Comment, Status
```

---

### `BarberSalon.Application`

> Contains: Use-cases (Services), DTOs, Repository Interfaces, External Service Interfaces.
> NO EF Core. NO HttpClient. NO external dependencies.

```
BarberSalon.Application/
├── Common/
│   ├── Interfaces/
│   │   ├── IUnitOfWork.cs          ← SaveChangesAsync()
│   │   ├── ISmsService.cs          ← SendAsync(phone, message)
│   │   └── ICurrentUser.cs         ← UserId, Role, IsAuthenticated
│   └── Exceptions/
│       ├── NotFoundException.cs
│       ├── ValidationException.cs
│       └── DomainException.cs
├── Auth/
│   ├── Interfaces/
│   │   ├── IUserRepository.cs
│   │   ├── IJwtService.cs          ← GenerateAccessToken, GenerateRefreshToken, Validate
│   │   └── IOtpService.cs          ← GenerateOtp, VerifyOtp, InvalidateOtp
│   ├── Services/
│   │   └── AuthService.cs          ← SendOtp, VerifyOtp, RefreshToken, Logout
│   └── DTOs/
│       ├── SendOtpRequest.cs
│       ├── VerifyOtpRequest.cs
│       └── AuthResponse.cs         ← AccessToken, RefreshToken, Role, ExpiresAt
├── Booking/
│   ├── Interfaces/
│   │   └── IAppointmentRepository.cs
│   ├── Services/
│   │   └── BookingService.cs
│   └── DTOs/
│       ├── AppointmentDto.cs
│       ├── CreateAppointmentRequest.cs
│       └── AvailableSlotDto.cs
├── Customers/
│   ├── Interfaces/
│   │   └── ICustomerRepository.cs
│   ├── Services/
│   │   └── CustomerService.cs
│   └── DTOs/
│       ├── CustomerDto.cs
│       └── CreateCustomerRequest.cs
├── Staff/
│   ├── Interfaces/
│   │   └── IStaffRepository.cs
│   ├── Services/
│   │   └── StaffService.cs
│   └── DTOs/
│       └── StaffDto.cs
├── SalonServices/
│   ├── Interfaces/
│   │   └── ISalonServiceRepository.cs
│   ├── Services/
│   │   └── SalonServiceManager.cs
│   └── DTOs/
│       ├── SalonServiceDto.cs
│       ├── CreateSalonServiceRequest.cs
│       └── UpdateSalonServiceRequest.cs
├── Loyalty/
│   ├── Interfaces/
│   │   └── ILoyaltyRepository.cs
│   ├── Services/
│   │   └── LoyaltyService.cs
│   └── DTOs/
│       └── LoyaltyAccountDto.cs
└── Waitlist/
    ├── Interfaces/
    │   └── IWaitlistRepository.cs
    ├── Services/
    │   └── WaitlistService.cs
    └── DTOs/
        └── WaitlistEntryDto.cs
```

---

### `BarberSalon.Infrastructure`

> Implements all Application interfaces.
> EF Core, Migrations, JWT, OTP storage, and external services (SMS) live here.

```
BarberSalon.Infrastructure/
├── Persistence/
│   ├── AppDbContext.cs
│   ├── Migrations/
│   └── Configurations/              ← EF Fluent API configs
│       ├── UserConfiguration.cs
│       ├── AppointmentConfiguration.cs
│       └── ...
├── Repositories/
│   ├── UserRepository.cs
│   ├── AppointmentRepository.cs
│   ├── CustomerRepository.cs
│   ├── StaffRepository.cs
│   ├── SalonServiceRepository.cs
│   ├── LoyaltyRepository.cs
│   └── WaitlistRepository.cs
├── Auth/
│   ├── JwtService.cs               ← Generate and validate JWT tokens
│   └── OtpService.cs               ← Generate, store, verify, expire OTP
├── ExternalServices/
│   └── SmsService.cs               ← Kavenegar / Melipayam integration
└── DependencyInjection.cs          ← All Infrastructure registrations
```

---

### `BarberSalon.API`

> HTTP layer only. Zero business logic allowed.

```
BarberSalon.API/
├── Controllers/
│   ├── AuthController.cs
│   ├── BookingController.cs
│   ├── AppointmentsController.cs
│   ├── CustomersController.cs
│   ├── StaffController.cs
│   ├── SalonServicesController.cs
│   └── LoyaltyController.cs
├── Middleware/
│   └── ExceptionHandlingMiddleware.cs
├── DependencyInjection.cs
├── appsettings.json
├── appsettings.Development.json
└── Program.cs
```

---

## §3 Project Dependencies

```
BarberSalon.API
    → BarberSalon.Application
    → BarberSalon.Infrastructure

BarberSalon.Application
    → BarberSalon.Domain

BarberSalon.Infrastructure
    → BarberSalon.Application   (implements its interfaces)
    → BarberSalon.Domain

BarberSalon.Domain
    → nothing (zero dependencies)
```

**Rule:** Domain never depends on Application or Infrastructure.
**Rule:** Application never depends on Infrastructure.

---

## §4 Pre-task Checklist

Run this checklist **before creating or modifying any file**. Answer every question. If any answer is "STOP", fix the design before writing code.

### A — Layer Check

```
1. Which layer does this file belong to?
   → Domain / Application / Infrastructure / API

2. Does it depend on anything from a higher layer?
   Domain importing Application?       → STOP — reverse the dependency
   Application importing Infrastructure? → STOP — use an interface instead

3. Does it import EF Core (DbContext, DbSet, IQueryable)?
   In Domain?      → STOP — move to Infrastructure
   In Application? → STOP — move to Infrastructure
```

### B — Content Check

```
4. Does it contain business logic (validation, state transitions, rules)?
   In Controller? → STOP — move to Domain Entity or Application Service
   In Repository? → STOP — move to Application Service

5. Does it contain a public setter on an Entity property?
   → STOP — make it private set; expose a method instead

6. Does it define a Repository Interface?
   In Domain?     → STOP — move to Application/<Module>/Interfaces/
```

### C — Pattern Check (compare to SalonService example)

```
7. New Entity?
   → Has Factory static method (Create) instead of public constructor?
   → All properties are private set?
   → Business rules throw DomainException inside the Entity?
   → Has private parameterless constructor for EF Core?

8. New Repository Interface?
   → Lives in Application/<Module>/Interfaces/?
   → Returns Domain Entities (not DTOs)?
   → Has CancellationToken on all async methods?

9. New Application Service?
   → Depends on IRepository interface (not concrete class)?
   → Calls _unitOfWork.SaveChangesAsync() after mutations?
   → Maps Entity → DTO before returning?
   → Throws NotFoundException when entity not found?

10. New Controller?
    → Has [ProducesResponseType] for EVERY possible status code?
    → Has XML <summary> on every action?
    → Has no business logic (only calls Service, returns result)?
    → Uses CancellationToken on every action?
```

### D — Forbidden Check

```
11. Does it introduce any of these? → STOP immediately

    MediatR                 → Use plain Service method instead
    AutoMapper              → Map manually in the Service
    ASP.NET Identity        → Use custom AuthService + JwtService
    Command / Query classes → Use plain Service methods
    public constructor on Entity (non-EF) → Use static Create() factory
    Hard delete (Remove())  → Use soft delete (Archive / IsActive = false)
```

---

## §5 Auth — Custom JWT + Phone OTP

### Flow

```
1. POST /api/auth/send-otp      { phoneNumber }
       ↓
   OtpService.Generate() → store in DB (OtpCodes table) TTL = 5 min
       ↓
   SmsService.Send(phoneNumber, code)

2. POST /api/auth/verify-otp    { phoneNumber, otpCode }
       ↓
   OtpService.Verify() → check code + expiry + IsUsed
       ↓
   JwtService.GenerateAccessToken(user) + GenerateRefreshToken()
       ↓
   Response: { accessToken (15 min), refreshToken (7 days), role }

3. POST /api/auth/refresh        { refreshToken }
       ↓
   Validate refreshToken from DB
       ↓
   Issue new accessToken
```

### OTP Table

`OtpCodes`: `Id`, `PhoneNumber`, `Code`, `ExpiresAt`, `IsUsed`, `CreatedAt`
- New request invalidates previous OTP for same phone
- Redis: not yet — add later when needed

### JWT Config (`appsettings.json`)

```json
"Jwt": {
  "Secret": "...",
  "AccessTokenExpiryMinutes": 15,
  "RefreshTokenExpiryDays": 7,
  "Issuer": "BarberSalon",
  "Audience": "BarberSalon"
}
```

---

## §6 DDD Lite — Rules

This project uses **DDD Lite** — not Full DDD.

### What IS used

| Concept | Example |
|---------|---------|
| Entity | `Appointment`, `Customer`, `SalonService` |
| Value Object | `TimeSlot`, `PhoneNumber` |
| Aggregate Root | `Appointment` (entry point to Booking aggregate) |
| Repository Interface | `IAppointmentRepository` in Application |
| Domain Exception | `DomainException` for rule violations |
| Factory method | `SalonService.Create(...)` |

### What is NOT used

| Concept | Reason |
|---------|--------|
| Domain Events | Unnecessary complexity at this scale |
| Event Sourcing | Overkill |
| Separate Bounded Contexts | Modular Monolith is sufficient |
| CQRS / MediatR | Overhead with no benefit |

### Correct vs Incorrect pattern

```csharp
// ✅ CORRECT — business rule inside Entity
public class Appointment : BaseEntity
{
    public AppointmentStatus Status { get; private set; }

    public void Cancel(string reason)
    {
        if (Status == AppointmentStatus.Completed)
            throw new DomainException("A completed appointment cannot be cancelled.");
        Status = AppointmentStatus.Cancelled;
        Touch();
    }
}

// ❌ INCORRECT — business rule leaked into Service
public class BookingService
{
    public async Task CancelAsync(Guid id)
    {
        var apt = await _repo.GetByIdAsync(id);
        if (apt.Status == AppointmentStatus.Completed) // ← belongs in Entity
            throw new Exception("...");
        apt.Status = AppointmentStatus.Cancelled;       // ← setter must be private
    }
}
```

---

## §7 EF Core — Rules

- **Code First**: start from the Entity, never from the database
- **Migrations** run only from the Infrastructure project:

```powershell
dotnet ef migrations add <Name> `
  --project src/BarberSalon.Infrastructure `
  --startup-project src/BarberSalon.API

dotnet ef database update `
  --project src/BarberSalon.Infrastructure `
  --startup-project src/BarberSalon.API
```

- **Fluent API** configuration in `Configurations/` — no DataAnnotations
- **Primary keys**: `Guid` for all entities
- **Soft delete**: set `IsActive = false` — never call `DbSet.Remove()`

---

## §8 Documentation Conventions

### XML Documentation (C# code)

All `public` classes, interfaces, enums, methods, and non-obvious properties **must** have XML docs.

```csharp
/// <summary>
/// Represents a bookable service offered by the salon (e.g. Haircut, Beard Trim).
/// Acts as an independent Aggregate Root.
/// </summary>
public sealed class SalonService : BaseEntity { }

/// <summary>
/// Archives the service, making it unavailable for new bookings.
/// </summary>
/// <exception cref="DomainException">Thrown when the service is already archived.</exception>
public void Archive() { }

/// <summary>
/// Returns all active services for a given customer, sorted by date descending.
/// </summary>
/// <param name="customerId">The customer identifier.</param>
/// <param name="cancellationToken"></param>
/// <returns>A list of <see cref="AppointmentDto"/> sorted by date descending.</returns>
Task<List<AppointmentDto>> GetActiveByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
```

**Rules:**
- English only for all XML docs
- Never start summary with "This method" — state what it does directly
- Use `<inheritdoc />` on implementations when the interface is documented
- Do NOT document trivial getters/setters

**Enable in `.csproj`:**

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <NoWarn>$(NoWarn);1591</NoWarn>
</PropertyGroup>
```

### API Documentation (Swagger)

Every Controller action must have:

```csharp
/// <summary>Sends a one-time password to the given phone number.</summary>
/// <remarks>
/// Each new request invalidates the previous OTP for the same number.
/// OTP expires after 5 minutes.
/// </remarks>
[HttpPost("send-otp")]
[AllowAnonymous]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request) { }
```

**Enable Swagger XML in `Program.cs`:**

```csharp
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "BarberSalon API", Version = "v1" });
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile));
});
```

---

## §9 Commit Conventions

Format: **Conventional Commits** — `<type>(<scope>): <subject>`

### Types

| Type | When to use |
|------|-------------|
| `feat` | New feature |
| `fix` | Bug fix |
| `docs` | Documentation only |
| `refactor` | Code change, no feature/fix |
| `test` | Add or fix tests |
| `chore` | Build, config, migrations |
| `style` | Formatting, no logic change |

### Scopes (project-specific)

| Scope | Module |
|-------|--------|
| `auth` | Auth module |
| `booking` | Booking / Appointment |
| `customer` | Customers |
| `staff` | Staff |
| `salon-service` | SalonServices |
| `loyalty` | Loyalty |
| `waitlist` | Waitlist |
| `infra` | Infrastructure layer |
| `db` | Migration, EF config |
| `api` | Controller, Middleware |
| `domain` | Entity, Value Object |

### Examples

```
feat(booking): add GetAvailableSlots to BookingService
feat(auth): implement OTP generation and DB storage
fix(booking): prevent double-booking on concurrent requests
chore(db): add migration for OtpCodes table
test(salon-service): add unit tests for Archive business rule
docs(api): add XML docs to SalonServicesController
refactor(infra): extract JWT config into extension method
```

### Rules

- Subject: imperative verb, lowercase first letter, no trailing period
- Max 72 characters for subject line
- Migration commits: always `chore(db): add migration for <EntityName>`
- Never mix unrelated changes in one commit
- Breaking change: add `BREAKING CHANGE: <description>` in commit body

---

## §10 Hard Rules for Agent

1. **Never** put business logic in a Controller
2. **Never** use EF Core (`DbContext`, `DbSet`) in Application or Domain
3. **Never** define Repository Interface in Domain — it belongs in Application
4. **Never** introduce CQRS or Command/Query classes
5. **Never** add MediatR or any mediator/dispatcher library
6. **Never** add ASP.NET Identity
7. **Never** add Docker or docker-compose files
8. **Never** use hard delete — always soft delete via `IsActive = false`
9. Entity setters must be `private set` — state changes only through methods
10. Every Module must follow: `Interfaces/` + `Services/` + `DTOs/` structure
11. Migrations run only from `Infrastructure` project
12. Every async method must accept `CancellationToken cancellationToken = default`

---

## §11 Forbidden NuGet Packages

| Package | Why forbidden | Approved alternative |
|---------|--------------|----------------------|
| `MediatR` | Introduces CQRS pattern — rejected | Plain Service method |
| `AutoMapper` | Hidden mapping magic; hard to debug | Manual mapping in Service |
| `Microsoft.AspNetCore.Identity.*` | Email/password-first; we use phone OTP | Custom `AuthService` + `JwtService` |
| `Hangfire` | Message queue — deferred | `IHostedService` for now |
| `FluentValidation` | Not needed for this scale | Validation inside Domain Entity |
| `Dapper` | Conflicts with EF Code First approach | EF Core only |

---

## §12 Related Frontend

- **Frontend project:** `e:\barber\beauty-saloon-front-V2` (Next.js 16, React 19)
- **Frontend docs:** `e:\barber\beauty-saloon-front-V2\docs\PROJECT_DOCUMENTATION.md`
- **Connection:** Frontend will replace mock data with this REST API
- **Auth:** Frontend uses phone OTP — same flow this backend implements

---

## §13 Reference Implementation

See the complete working example for all layers:

📄 `docs/examples/SalonService-complete.md`

When implementing a new module, read the example first and follow its patterns exactly.
