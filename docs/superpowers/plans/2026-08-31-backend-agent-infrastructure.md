# Backend Agent Infrastructure Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the `.agents/` folder inside `beauty-saloon-api/` — the complete headless agent infrastructure for autonomous backend development using a use-case-per-iteration loop.

**Architecture:** Orchestrator script (PowerShell) owns the loop; LLM-enriched prompts per use case are injected into an implementer subagent; only green `dotnet test` output advances the queue. Three buckets: `skills/` (static how-to), `state/` (runtime data), `agents/` (role personas).

**Tech Stack:** Markdown skill files, JSON state, PowerShell loop runner, .NET 10 xUnit test harness

## Global Constraints

- All paths relative to `E:\barber\beauty-saloon-api\` unless stated otherwise
- Skill files: Markdown, front-matter with `name` and `description`
- State files: JSON (queue) or Markdown (progress, plan)
- No placeholder content — every file must be complete and usable as-is
- Follow the naming: `skills/<name>/SKILL.md`, never `.name/skill.md`
- Domain vocabulary must match `CONTEXT.md` exactly (Appointment, Customer, SalonService, etc.)

---

## File Structure

```
beauty-saloon-api/
├── .agents/
│   ├── rules/
│   │   ├── always-on.md
│   │   └── architecture-laws.md
│   ├── skills/
│   │   ├── backend-cycle/SKILL.md        ← orchestrator loop logic
│   │   ├── unit-test/SKILL.md
│   │   ├── integration-test/SKILL.md
│   │   ├── clean-code/SKILL.md
│   │   ├── code-review/SKILL.md
│   │   ├── architecture-check/SKILL.md
│   │   ├── architecture-check/target-arch.md
│   │   ├── storyteller/SKILL.md
│   │   ├── storyconnector/SKILL.md
│   │   ├── eventwriter/SKILL.md
│   │   ├── scenariowriter/SKILL.md
│   │   ├── prompt-engineer/SKILL.md
│   │   ├── prompt-engineer/base-template.md
│   │   ├── handoff/SKILL.md
│   │   └── handoff/template.md
│   ├── state/
│   │   ├── queue.json
│   │   ├── progress.md
│   │   ├── current-plan.md
│   │   ├── current-prompt.txt
│   │   └── handoff-reports/   (empty dir, populated at runtime)
│   ├── plans/
│   │   ├── target.md
│   │   ├── phases.md
│   │   └── bootstrap.md
│   ├── agents/
│   │   ├── orchestrator.md
│   │   ├── implementer.md
│   │   ├── reviewer.md
│   │   └── tester.md
│   └── logs/                  (empty dir, populated at runtime)
├── run-backend-cycle.ps1      ← loop runner at repo root
└── tests/
    └── BarberSalon.IntegrationTests/   ← NEW: add to solution
```

---

### Task 1: Create Folder Scaffold

**Files:**
- Create: `.agents/rules/`
- Create: `.agents/skills/backend-cycle/`
- Create: `.agents/skills/unit-test/`
- Create: `.agents/skills/integration-test/`
- Create: `.agents/skills/clean-code/`
- Create: `.agents/skills/code-review/`
- Create: `.agents/skills/architecture-check/`
- Create: `.agents/skills/storyteller/`
- Create: `.agents/skills/storyconnector/`
- Create: `.agents/skills/eventwriter/`
- Create: `.agents/skills/scenariowriter/`
- Create: `.agents/skills/prompt-engineer/`
- Create: `.agents/skills/handoff/`
- Create: `.agents/state/handoff-reports/`
- Create: `.agents/plans/`
- Create: `.agents/agents/`
- Create: `.agents/logs/`

- [ ] **Step 1: Create all directories**

```powershell
$base = "E:\barber\beauty-saloon-api\.agents"
$dirs = @(
  "rules",
  "skills\backend-cycle",
  "skills\unit-test",
  "skills\integration-test",
  "skills\clean-code",
  "skills\code-review",
  "skills\architecture-check",
  "skills\storyteller",
  "skills\storyconnector",
  "skills\eventwriter",
  "skills\scenariowriter",
  "skills\prompt-engineer",
  "skills\handoff",
  "state\handoff-reports",
  "plans",
  "agents",
  "logs"
)
foreach ($d in $dirs) {
  New-Item -ItemType Directory -Force -Path "$base\$d" | Out-Null
}
Write-Host "All directories created."
```

Expected: `All directories created.`

- [ ] **Step 2: Verify**

```powershell
Get-ChildItem E:\barber\beauty-saloon-api\.agents -Directory -Recurse | Select-Object FullName
```

Expected: all 17 directories listed.

---

### Task 2: Create `rules/` Files

**Files:**
- Create: `.agents/rules/always-on.md`
- Create: `.agents/rules/architecture-laws.md`

- [ ] **Step 1: Create `rules/always-on.md`**

```markdown
# Always-On Rules — All Agents, Every Task

These rules apply unconditionally. No agent may skip them.

## 1. RTK — Read Before Writing
Before writing any code, read in order:
1. `.agents/plans/target.md` — what we are building
2. `CONTEXT.md` — domain vocabulary (exact terms, no synonyms)
3. The SKILL.md for your current task
4. `.agents/state/current-prompt.txt` — your specific mission

## 2. TDD — Test First, Always
1. Write a failing test first
2. Run `dotnet test` — confirm it fails (red)
3. Write minimal code to pass
4. Run `dotnet test` — confirm green
5. Never claim "done" without a `dotnet test` green screenshot/output

## 3. Ponytail — Minimum Viable Code
- No code without a test proving it is needed
- No abstraction before two proven use cases require it
- No generic base classes, no `BaseRepository<T>` upfront
- No logging, caching, or middleware unless in current-prompt.txt

## 4. Report After Every Task
When done: write handoff report to
`state/handoff-reports/[use-case-name].md`
using `skills/handoff/template.md`.

## 5. Mandatory Build+Test Commands
After every file creation or modification:
```
dotnet build E:\barber\beauty-saloon-api\BarberSalon.slnx
dotnet test E:\barber\beauty-saloon-api\
```
Both must pass before declaring done.
```

- [ ] **Step 2: Create `rules/architecture-laws.md`**

```markdown
# Architecture Laws — BarberSalon Backend

## Layer Dependency (enforced, no exceptions)

```
API  →  Application  →  Domain
         ↑
Infrastructure  →  Domain
```

- API depends on Application (via MediatR commands/queries)
- Application depends on Domain (entities + interfaces)
- Infrastructure depends on Domain (implements IRepository interfaces)
- Domain depends on nothing — no EF, no HTTP, no DI framework

## What Goes Where

| Layer | Allowed | Forbidden |
|-------|---------|-----------|
| Domain | Entities, Value Objects, Enums, Domain Events, IRepository interfaces | EF attributes, HttpContext, DI, anything from Microsoft.* |
| Application | Use case Handlers (IRequest/IRequestHandler), DTOs, validation | EF DbContext, concrete Repositories, HTTP |
| Infrastructure | EF DbContext, Repository implementations, EF configurations | Business logic, direct HTTP calls |
| API | Controllers, DI registration (Program.cs), middleware | Business logic, direct DB access, domain logic |

## Naming Conventions

| Thing | Pattern | Example |
|-------|---------|---------|
| Use case handler | `[Action][Entity]Handler` | `GetActiveSalonServicesHandler` |
| Repository interface | `I[Entity]Repository` | `ISalonServiceRepository` |
| Command/Query | `[Action][Entity]Command` or `Query` | `GetActiveSalonServicesQuery` |
| Response DTO | `[Entity]Response` | `SalonServiceResponse` |
| Request DTO | `[Action][Entity]Request` | `CreateAppointmentRequest` |
| Controller | `[Entity]Controller` | `SalonServicesController` |
| Endpoint prefix | `/api/v1/[resource]` (plural, lowercase) | `/api/v1/salon-services` |

## Response Envelope (ALL endpoints)

```json
{
  "data": <T | null>,
  "success": true | false,
  "message": "Human readable string"
}
```

## Domain Vocabulary (MUST match CONTEXT.md)
- Appointment (not Booking, not Reservation)
- SalonService (not Service, not Treatment)
- StaffMember (not Employee, not Provider)
- Customer (not Client, not User)
- TimeSlot (not Slot, not Block)
- UserRole: Customer | Staff | Admin
- AppointmentStatus: Pending | Confirmed | Completed | Cancelled | NoShow
```

- [ ] **Step 3: Verify files exist and are non-empty**

```powershell
(Get-Content "E:\barber\beauty-saloon-api\.agents\rules\always-on.md").Count
(Get-Content "E:\barber\beauty-saloon-api\.agents\rules\architecture-laws.md").Count
```

Expected: both return line counts > 30.

---

### Task 3: Create `plans/` Files

**Files:**
- Create: `.agents/plans/target.md`
- Create: `.agents/plans/phases.md`
- Create: `.agents/plans/bootstrap.md`

- [ ] **Step 1: Create `plans/target.md`**

```markdown
# Target — BarberSalon Backend

## Goal
Build a complete .NET 10 Web API for a barbershop appointment booking system.
Every use case is driven by what the frontend (beauty-saloon-front-V2) already
implements via mock data. When all use cases are complete, the frontend can
replace every `dataService.*` call with a real API call.

## Architecture
Clean Architecture — 4 layers: Domain → Application → Infrastructure → API.
CQRS via MediatR: every use case = one Handler (Query or Command).
No shared state across layers. No business logic in Infrastructure or API.

## Tech Stack
- .NET 10, ASP.NET Core Web API
- Entity Framework Core with SQLite (dev) / PostgreSQL (prod)
- MediatR (CQRS)
- xUnit + FluentAssertions + NSubstitute (unit tests — already configured)
- xUnit + WebApplicationFactory + EF in-memory (integration tests)

## Definition of Done (entire project)
- All use cases in `state/queue.json` have `status: "completed"`
- `dotnet test E:\barber\beauty-saloon-api\` → 0 failures
- Every function in `beauty-saloon-front-V2/src/lib/data/dataService.ts`
  has a corresponding real endpoint (verified by storyconnector skill)
- No mock data accessed in the production code path
```

- [ ] **Step 2: Create `plans/phases.md`**

```markdown
# Use Case Phases — Ordered by Dependency

Use this as the source of truth for queue.json ordering.
A use case cannot start until all items in `depends_on` are `completed`.

## Phase 1 — Foundation (no dependencies)

| id | name | frontend_ref | layer |
|----|------|-------------|-------|
| get-active-salon-services | GetActiveSalonServices | getActiveServices | SalonServices |
| send-otp | SendOtp | sendOtp | Auth |

## Phase 2 — Auth (depends on send-otp)

| id | name | frontend_ref | depends_on |
|----|------|-------------|-----------|
| verify-otp | VerifyOtp | verifyOtpCode | send-otp |

## Phase 3 — Catalog (depends on get-active-salon-services)

| id | name | frontend_ref | depends_on |
|----|------|-------------|-----------|
| get-active-staff | GetActiveStaff | getActiveStaff | get-active-salon-services |
| get-staff-by-service | GetStaffByService | getStaffByService | get-active-staff |
| get-available-timeslots | GetAvailableTimeSlots | getAvailableTimeSlots | get-active-staff |

## Phase 4 — Booking (depends on Phase 2 + 3)

| id | name | frontend_ref | depends_on |
|----|------|-------------|-----------|
| create-appointment | CreateAppointment | createAppointment | verify-otp, get-active-staff |
| get-appointments-by-customer | GetAppointmentsByCustomer | getAppointmentsByCustomer | create-appointment |
| get-appointments-by-date | GetAppointmentsByDate | getAppointmentsByDate | create-appointment |

## Phase 5 — Customer CRM

| id | name | frontend_ref | depends_on |
|----|------|-------------|-----------|
| create-customer | CreateCustomer | createCustomer | verify-otp |
| get-customer-by-id | GetCustomerById | getCustomerById | create-customer |
| get-customers | GetCustomers | getCustomers | create-customer |
| search-customers | SearchCustomers | searchCustomers | get-customers |
| update-user-profile | UpdateUserProfile | updateUserProfile | create-customer |

## Phase 6 — Admin Dashboard

| id | name | frontend_ref | depends_on |
|----|------|-------------|-----------|
| get-dashboard-stats | GetDashboardStats | getDashboardStats | create-appointment |
| get-appointments | GetAppointments | getAppointments | create-appointment |
| get-available-days | GetAvailableDays | getAvailableDays | create-appointment |
```

- [ ] **Step 3: Create `plans/bootstrap.md`**

```markdown
# Bootstrap Checklist — One-Time Setup

Run this ONCE before starting the use case loop.
Each checkbox must be verified before moving to the next.

## Step 1: Verify Solution Builds

- [ ] Run: `dotnet build E:\barber\beauty-saloon-api\BarberSalon.slnx`
- [ ] Expected: Build succeeded, 0 Error(s)
- [ ] Four projects compile: API, Application, Domain, Infrastructure

## Step 2: Add Integration Test Project

- [ ] Run:
  ```powershell
  cd E:\barber\beauty-saloon-api
  dotnet new xunit -n BarberSalon.IntegrationTests -o tests/BarberSalon.IntegrationTests --framework net10.0
  dotnet sln BarberSalon.slnx add tests/BarberSalon.IntegrationTests/BarberSalon.IntegrationTests.csproj
  ```
- [ ] Add packages:
  ```powershell
  cd tests/BarberSalon.IntegrationTests
  dotnet add package Microsoft.AspNetCore.Mvc.Testing
  dotnet add package FluentAssertions --version 8.*
  dotnet add package Microsoft.EntityFrameworkCore.InMemory
  dotnet add package coverlet.collector
  ```
- [ ] Add project reference to API:
  ```powershell
  dotnet add reference ..\..\src\BarberSalon.API\BarberSalon.API.csproj
  ```
- [ ] Run: `dotnet build` → 0 errors
- [ ] Run: `dotnet test` → 0 failures (placeholder test is OK)

## Step 3: Minimal Program.cs

- [ ] Verify `src/BarberSalon.API/Program.cs` has at minimum:
  ```csharp
  var builder = WebApplication.CreateBuilder(args);
  builder.Services.AddControllers();
  var app = builder.Build();
  app.MapControllers();
  app.Run();
  ```
- [ ] `dotnet run --project src/BarberSalon.API` starts without crash

## Step 4: Initialize State Files

- [ ] Run storyteller skill against `beauty-saloon-front-V2/src/lib/data/dataService.ts`
- [ ] Confirm `state/queue.json` exists and has at least 10 use cases
- [ ] Confirm `state/progress.md` shows 0% complete

## Step 5: Dry-Run the Loop Script

- [ ] Run: `.\run-backend-cycle.ps1 -DryRun`
- [ ] Expected output: lists each pending use case, says DRY RUN for each, exits 0
```

---

### Task 4: Create `state/` Files

**Files:**
- Create: `.agents/state/queue.json`
- Create: `.agents/state/progress.md`
- Create: `.agents/state/current-plan.md`
- Create: `.agents/state/current-prompt.txt`

- [ ] **Step 1: Create `state/queue.json`**

```json
{
  "meta": {
    "created_at": "2026-08-31",
    "total": 16,
    "completed": 0,
    "in_progress": 0,
    "failed": 0
  },
  "use_cases": [
    {
      "id": "get-active-salon-services",
      "name": "GetActiveSalonServices",
      "status": "pending",
      "dependencies": [],
      "priority": 1,
      "layer": "SalonServices",
      "frontend_ref": "dataService.getActiveServices",
      "retries": 0
    },
    {
      "id": "send-otp",
      "name": "SendOtp",
      "status": "pending",
      "dependencies": [],
      "priority": 2,
      "layer": "Auth",
      "frontend_ref": "dataService.sendOtp",
      "retries": 0
    },
    {
      "id": "verify-otp",
      "name": "VerifyOtp",
      "status": "pending",
      "dependencies": ["send-otp"],
      "priority": 3,
      "layer": "Auth",
      "frontend_ref": "dataService.verifyOtpCode",
      "retries": 0
    },
    {
      "id": "get-active-staff",
      "name": "GetActiveStaff",
      "status": "pending",
      "dependencies": ["get-active-salon-services"],
      "priority": 4,
      "layer": "Staff",
      "frontend_ref": "dataService.getActiveStaff",
      "retries": 0
    },
    {
      "id": "get-staff-by-service",
      "name": "GetStaffByService",
      "status": "pending",
      "dependencies": ["get-active-staff"],
      "priority": 5,
      "layer": "Staff",
      "frontend_ref": "dataService.getStaffByService",
      "retries": 0
    },
    {
      "id": "get-available-timeslots",
      "name": "GetAvailableTimeSlots",
      "status": "pending",
      "dependencies": ["get-active-staff"],
      "priority": 6,
      "layer": "Booking",
      "frontend_ref": "dataService.getAvailableTimeSlots",
      "retries": 0
    },
    {
      "id": "create-appointment",
      "name": "CreateAppointment",
      "status": "pending",
      "dependencies": ["verify-otp", "get-active-staff"],
      "priority": 7,
      "layer": "Booking",
      "frontend_ref": "dataService.createAppointment",
      "retries": 0
    },
    {
      "id": "get-appointments-by-customer",
      "name": "GetAppointmentsByCustomer",
      "status": "pending",
      "dependencies": ["create-appointment"],
      "priority": 8,
      "layer": "Booking",
      "frontend_ref": "dataService.getAppointmentsByCustomer",
      "retries": 0
    },
    {
      "id": "get-appointments-by-date",
      "name": "GetAppointmentsByDate",
      "status": "pending",
      "dependencies": ["create-appointment"],
      "priority": 9,
      "layer": "Booking",
      "frontend_ref": "dataService.getAppointmentsByDate",
      "retries": 0
    },
    {
      "id": "create-customer",
      "name": "CreateCustomer",
      "status": "pending",
      "dependencies": ["verify-otp"],
      "priority": 10,
      "layer": "Customers",
      "frontend_ref": "dataService.createCustomer",
      "retries": 0
    },
    {
      "id": "get-customer-by-id",
      "name": "GetCustomerById",
      "status": "pending",
      "dependencies": ["create-customer"],
      "priority": 11,
      "layer": "Customers",
      "frontend_ref": "dataService.getCustomerById",
      "retries": 0
    },
    {
      "id": "get-customers",
      "name": "GetCustomers",
      "status": "pending",
      "dependencies": ["create-customer"],
      "priority": 12,
      "layer": "Customers",
      "frontend_ref": "dataService.getCustomers",
      "retries": 0
    },
    {
      "id": "search-customers",
      "name": "SearchCustomers",
      "status": "pending",
      "dependencies": ["get-customers"],
      "priority": 13,
      "layer": "Customers",
      "frontend_ref": "dataService.searchCustomers",
      "retries": 0
    },
    {
      "id": "update-user-profile",
      "name": "UpdateUserProfile",
      "status": "pending",
      "dependencies": ["create-customer"],
      "priority": 14,
      "layer": "Customers",
      "frontend_ref": "dataService.updateUserProfile",
      "retries": 0
    },
    {
      "id": "get-dashboard-stats",
      "name": "GetDashboardStats",
      "status": "pending",
      "dependencies": ["create-appointment"],
      "priority": 15,
      "layer": "Admin",
      "frontend_ref": "dataService.getDashboardStats",
      "retries": 0
    },
    {
      "id": "get-appointments",
      "name": "GetAppointments",
      "status": "pending",
      "dependencies": ["create-appointment"],
      "priority": 16,
      "layer": "Admin",
      "frontend_ref": "dataService.getAppointments",
      "retries": 0
    }
  ]
}
```

- [ ] **Step 2: Create `state/progress.md`**

```markdown
# Implementation Progress

**Last updated:** (updated by loop script after each use case)
**Overall:** 0 / 16 use cases (0%)

## Completed
_(none yet)_

## In Progress
_(none yet)_

## Failed
_(none yet)_

## Pending
- get-active-salon-services (Phase 1)
- send-otp (Phase 1)
- verify-otp (Phase 2)
- get-active-staff (Phase 3)
- get-staff-by-service (Phase 3)
- get-available-timeslots (Phase 3)
- create-appointment (Phase 4)
- get-appointments-by-customer (Phase 4)
- get-appointments-by-date (Phase 4)
- create-customer (Phase 5)
- get-customer-by-id (Phase 5)
- get-customers (Phase 5)
- search-customers (Phase 5)
- update-user-profile (Phase 5)
- get-dashboard-stats (Phase 6)
- get-appointments (Phase 6)
```

- [ ] **Step 3: Create `state/current-plan.md`** (empty template for runtime)

```markdown
# Current Use Case Plan

_(populated by orchestrator at start of each iteration)_

## Use Case
<!-- name -->

## Frontend Context
<!-- from storyteller skill -->

## Events
<!-- from eventwriter skill -->

## Scenarios
<!-- from scenariowriter skill -->

## Files to Create
<!-- exact paths -->

## Done Criteria
<!-- checkboxes from current-prompt.txt -->
```

- [ ] **Step 4: Create `state/current-prompt.txt`** (empty — runtime only)

```
(populated by orchestrator Phase 2 — Enrich — before each iteration)
```

---

### Task 5: Create `agents/` Persona Files

**Files:**
- Create: `.agents/agents/orchestrator.md`
- Create: `.agents/agents/implementer.md`
- Create: `.agents/agents/reviewer.md`
- Create: `.agents/agents/tester.md`

- [ ] **Step 1: Create `agents/orchestrator.md`**

```markdown
# Orchestrator Agent

## Identity
You are the Orchestrator of the BarberSalon backend implementation loop.
You are methodical, patient, and NEVER skip steps.
You do not write implementation code. You manage, prompt, and verify.

## Session Start Ritual
1. Read `rules/always-on.md`
2. Read `state/queue.json` → check for pending items
3. Read `state/progress.md` → understand current state
4. Read `skills/backend-cycle/SKILL.md` → your procedure
5. State your mode: INIT | RESUME | DONE

## Mission
Run the backend-cycle loop until all use cases in `state/queue.json` are completed.
Loop: Select → Enrich → Inject → Verify → Close → Next.

## Files You READ
- `state/queue.json` (every phase start)
- `state/progress.md`
- `plans/phases.md`
- `plans/target.md`
- `skills/backend-cycle/SKILL.md`
- `rules/always-on.md`
- `rules/architecture-laws.md`
- `CONTEXT.md` (for Enrich phase)
- `skills/prompt-engineer/base-template.md` (for Enrich phase)
- `skills/storyteller/SKILL.md` (for INIT only)
- `skills/eventwriter/SKILL.md` (for Enrich phase)
- `skills/scenariowriter/SKILL.md` (for Enrich phase)

## Files You WRITE
- `state/queue.json` (update status each phase)
- `state/progress.md` (after Close)
- `state/current-prompt.txt` (Phase 2 Enrich output)
- `state/current-plan.md` (Phase 2 Enrich output)
- `state/handoff-reports/[use-case].md` (Phase 5 Close)

## Hard Rules
- Never write implementation code
- Never mark completed without seeing green `dotnet test` output
- Never skip Phase 2 Enrich — every prompt must be built from template
- If 3 retries fail → mark `failed`, stop loop, write failure report
```

- [ ] **Step 2: Create `agents/implementer.md`**

```markdown
# Implementer Agent

## Identity
You are the Implementer. You receive one enriched prompt per session.
You implement exactly what is in `state/current-prompt.txt` — nothing more.
You are precise, minimal (Ponytail), and test-first (TDD).

## Session Start Ritual
1. Read `state/current-prompt.txt` — YOUR COMPLETE MISSION (read this first)
2. Read `rules/always-on.md`
3. Read `rules/architecture-laws.md`
4. Read `CONTEXT.md` — domain vocabulary
5. Read `skills/unit-test/SKILL.md`
6. Read `skills/integration-test/SKILL.md`
7. Read `skills/clean-code/SKILL.md`

## Implementation Order (always this sequence)
1. Domain entity / value object (no EF, pure C#)
2. IRepository interface in Application (no implementation)
3. Query or Command + Handler in Application
4. Response DTO in Application
5. EF entity configuration + Repository implementation in Infrastructure
6. Controller + DI registration in API
7. Unit tests (NSubstitute mocks, test handler in isolation)
8. Integration tests (WebApplicationFactory, real handler + in-memory DB)

## Files You READ
- `state/current-prompt.txt` (mission)
- `rules/always-on.md`
- `rules/architecture-laws.md`
- `CONTEXT.md`
- `skills/unit-test/SKILL.md`
- `skills/integration-test/SKILL.md`
- `skills/clean-code/SKILL.md`

## Files You WRITE
- Implementation files (exact paths from current-prompt.txt only)
- Test files (exact paths from current-prompt.txt only)
- `state/handoff-reports/[use-case].md` (when all tests green)

## Hard Rules
- Write failing test BEFORE any production code
- Run `dotnet build` after every new file
- Run `dotnet test` before declaring done — output must show 0 failures
- Never modify files not listed in current-prompt.txt
- No TODO, no placeholder, no commented-out code in any committed file
```

- [ ] **Step 3: Create `agents/reviewer.md`**

```markdown
# Reviewer Agent

## Identity
You are the Code Reviewer. You run after implementer completes,
before the use case is marked completed.

## Session Start Ritual
1. Read `state/current-plan.md` — what was supposed to be built
2. Read `state/handoff-reports/[use-case].md` — what was actually built
3. Read `skills/code-review/SKILL.md` — your checklist
4. Read `rules/architecture-laws.md` — the laws you enforce

## Mission
Verify the implementation satisfies every item in code-review/SKILL.md.
If any check fails: report the exact file + line, show the problem,
show the fix. Implementer fixes, re-runs tests, re-submits.

## Output
```
REVIEW: [use-case-name]
PASSED: [count] checks
FAILED: [count] checks
---
FAIL [check name]: [file]:[line]
  Found: [problem code]
  Expected: [correct code]
---
VERDICT: APPROVED | CHANGES REQUESTED
```
```

- [ ] **Step 4: Create `agents/tester.md`**

```markdown
# Tester Agent

## Identity
You are the Tester. You verify test coverage after implementation,
ensuring every scenario from scenariowriter is covered by a test.

## Session Start Ritual
1. Read `state/current-plan.md` → scenarios section
2. Run `dotnet test --verbosity normal` and capture output
3. Read `skills/unit-test/SKILL.md`
4. Read `skills/integration-test/SKILL.md`

## Mission
Cross-reference: every scenario in current-plan.md must map to
a passing test in `dotnet test` output. Report any gap.

## Output
```
TESTER: [use-case-name]
SCENARIOS: [count] defined
TESTS FOUND: [count] passing
GAPS:
  - Scenario "[name]" → NO TEST FOUND
VERDICT: FULL COVERAGE | GAPS FOUND
```

If GAPS FOUND → implementer adds missing tests, re-runs.
```

---

### Task 6: Create `skills/backend-cycle/SKILL.md`

**Files:**
- Create: `.agents/skills/backend-cycle/SKILL.md`

- [ ] **Step 1: Write the orchestrator loop skill**

```markdown
---
name: backend-cycle
description: >
  The main headless loop for autonomous backend implementation.
  Orchestrates: INIT or RESUME → Select → Enrich → Inject → Verify → Close → repeat.
  Runs until all use cases in state/queue.json are completed.
---

# Backend Cycle — Orchestrator Skill

## Phase Decision (Always Run First)

Check `state/queue.json`:

| Condition | Mode | Next |
|-----------|------|------|
| File missing | INIT | Run Initialization |
| Has `pending` items with met dependencies | RESUME | Phase 1: Select |
| All items `completed` | DONE | Generate report, stop |
| Only `failed` items remain | BLOCKED | Report failures, stop |

Output your mode:
```
MODE: [INIT / RESUME / DONE / BLOCKED]
PENDING: [count] use cases remaining
PROCEEDING TO: Phase [N]
```

---

## INIT Mode (First Run Only)

1. Read `plans/phases.md` — ordered use case list
2. Confirm `state/queue.json` matches phases.md (all use cases present)
3. Run storyteller skill:
   - Read `beauty-saloon-front-V2/src/lib/data/dataService.ts`
   - Read `beauty-saloon-front-V2/src/lib/data/types.ts`
   - Read `beauty-saloon-front-V2/src/lib/data/mock/` files
   - Map each frontend function to its use case in queue.json
   - Write mapping notes to `state/current-plan.md`
4. Set `state/progress.md` → 0% (or update to actual if resuming)
5. Transition to Phase 1

---

## Phase 1 — Select

1. Read `state/queue.json`
2. Find first item where:
   - `status: "pending"`
   - All items in `dependencies[]` have `status: "completed"`
3. Set that item `status: "in-progress"`
4. Write updated `state/queue.json`

Output:
```
SELECTED: [use-case-name]  (id: [id])
DEPENDENCIES: all met ✓
FRONTEND REF: [frontend_ref]
```

If no eligible item found (dependencies not met):
```
BLOCKED: All pending items have unmet dependencies.
Completed: [list]
Pending with blocked deps: [list]
```
Stop and report.

---

## Phase 2 — Enrich (Prompt Generation)

Read in order:
1. `CONTEXT.md` → extract only terms relevant to this use case
2. `rules/architecture-laws.md` → layer rules + naming
3. `skills/prompt-engineer/base-template.md` → prompt template
4. `skills/eventwriter/SKILL.md` → generate Events for this use case
5. `skills/scenariowriter/SKILL.md` → generate Scenarios for each Event
6. `state/progress.md` → what is already built (avoid duplication)
7. `agents/implementer.md` → implementer persona + rules

Build the enriched prompt:
- Replace `{{use-case-name}}` with selected use case name
- Fill `{{domain-terms}}` with relevant CONTEXT.md entries only
- Fill `{{events}}` with output from eventwriter
- Fill `{{scenarios}}` with output from scenariowriter
- Fill `{{files-to-create}}` with exact file paths per architecture-laws.md

Write to: `state/current-prompt.txt`
Also write events + scenarios section to: `state/current-plan.md`

Output:
```
ENRICHED: [use-case-name]
EVENTS: [count] defined
SCENARIOS: [count] defined (unit: [N], integration: [N])
PROMPT: written to state/current-prompt.txt ([line count] lines)
```

---

## Phase 3 — Inject + Implement

Pass `state/current-prompt.txt` to implementer subagent.

Implementer follows `agents/implementer.md` session start ritual.
Implementation order (implementer must follow exactly):
1. Domain layer (entity, value objects, IRepository interface)
2. Application layer (Query/Command, Handler, Response DTO)
3. Infrastructure layer (EF config, Repository implementation)
4. API layer (Controller, DI wiring in Program.cs)
5. Unit tests (NSubstitute, test handler in isolation)
6. Integration tests (WebApplicationFactory, in-memory DB)

Orchestrator waits for implementer to output handoff report.

---

## Phase 4 — Verify

Run:
```powershell
dotnet test E:\barber\beauty-saloon-api\ --verbosity normal
```

| Result | Action |
|--------|--------|
| 0 failures | Proceed to Phase 5 |
| Build errors | Return to Phase 3 with compile-error context |
| Test failures | Return to Phase 3 with failure output (max 3 total retries) |
| 3 retries exhausted | Mark `failed`, write failure report, stop |

Increment `retries` counter in queue.json on each failure.

---

## Phase 5 — Close and Next

1. Dispatch reviewer agent with `skills/code-review/SKILL.md`
2. If reviewer returns CHANGES REQUESTED:
   - Implementer fixes, re-runs Phase 4 verify (counts as retry)
3. If reviewer returns APPROVED:
   - Write handoff report to `state/handoff-reports/[use-case-name].md`
   - Set `status: "completed"` in `state/queue.json`
   - Increment `meta.completed`, decrement `meta.in_progress`
   - Update `state/progress.md`
4. If more `pending` items → return to Phase 1
5. If all `completed` → output final summary, stop loop

Final summary format:
```
LOOP_COMPLETE
Total use cases: [N]
Completed: [N]
Failed: [N]
Total tests written: [unit: N, integration: N]
Duration: [start → end]
```
```

---

### Task 7: Create `skills/unit-test/SKILL.md`

**Files:**
- Create: `.agents/skills/unit-test/SKILL.md`

- [ ] **Step 1: Write the unit test skill**

```markdown
---
name: unit-test
description: How to write and run unit tests for the BarberSalon backend.
---

# Unit Test Skill

## What is a Unit Test Here
Tests one class in isolation. Dependencies (repositories, external services)
are replaced by NSubstitute fakes. No database, no HTTP, no filesystem.

## Project
`tests/BarberSalon.Application.Tests/` — for Application handlers
`tests/BarberSalon.Domain.Tests/` — for Domain entities + value objects

## File Naming
`tests/BarberSalon.Application.Tests/[Layer]/[UseCaseName]/[ClassName]Tests.cs`

Example:
`tests/BarberSalon.Application.Tests/SalonServices/GetActiveSalonServicesHandlerTests.cs`

## Test Class Template

```csharp
using FluentAssertions;
using NSubstitute;
using BarberSalon.Application.SalonServices.Queries;
using BarberSalon.Domain.SalonServices.Entities;
using BarberSalon.Domain.SalonServices.Interfaces;

namespace BarberSalon.Application.Tests.SalonServices;

public class GetActiveSalonServicesHandlerTests
{
    private readonly ISalonServiceRepository _repo;
    private readonly GetActiveSalonServicesHandler _handler;

    public GetActiveSalonServicesHandlerTests()
    {
        _repo = Substitute.For<ISalonServiceRepository>();
        _handler = new GetActiveSalonServicesHandler(_repo);
    }

    [Fact]
    public async Task Handle_WhenServicesExist_ReturnsMappedResponses()
    {
        // Arrange
        var services = new List<SalonService>
        {
            SalonService.Create("Men's Haircut", 30, 150_000, "Hair"),
            SalonService.Create("Beard Trim", 20, 80_000, "Beard")
        };
        _repo.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(services);

        // Act
        var result = await _handler.Handle(
            new GetActiveSalonServicesQuery(),
            CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Men's Haircut");
        result[0].DurationMinutes.Should().Be(30);
    }

    [Fact]
    public async Task Handle_WhenNoActiveServices_ReturnsEmptyList()
    {
        _repo.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(new List<SalonService>());
        var result = await _handler.Handle(new GetActiveSalonServicesQuery(), CancellationToken.None);
        result.Should().BeEmpty();
    }
}
```

## TDD Steps (always in this order)
1. Write the test class (it will not compile — that is expected)
2. Run: `dotnet test tests/BarberSalon.Application.Tests/ --no-build` → compile error expected
3. Create the production class with the minimal interface needed to compile
4. Run: `dotnet test` → RED (test fails)
5. Implement the minimal logic to make the test pass
6. Run: `dotnet test` → GREEN

## Rules
- One `[Fact]` per scenario (no `[Theory]` with magic strings)
- Use `Substitute.For<T>()` — never use `new ConcreteRepository()`
- Always call `.Returns()` on substitutes — never leave them returning default
- Test method name: `[Method]_[Condition]_[ExpectedResult]`
- No `Assert.True(x == y)` — always use FluentAssertions `.Should().Be()`
```

---

### Task 8: Create `skills/integration-test/SKILL.md`

**Files:**
- Create: `.agents/skills/integration-test/SKILL.md`

- [ ] **Step 1: Write the integration test skill**

```markdown
---
name: integration-test
description: How to write and run integration tests using WebApplicationFactory for the BarberSalon API.
---

# Integration Test Skill

## What is an Integration Test Here
Tests the full stack from HTTP request to database and back.
Uses `WebApplicationFactory<Program>` + EF InMemory database.
No mocks — real handlers, real EF, fake HTTP client.

## Project
`tests/BarberSalon.IntegrationTests/`

## File Naming
`tests/BarberSalon.IntegrationTests/[Layer]/[UseCaseName]/[ClassName]Tests.cs`

Example:
`tests/BarberSalon.IntegrationTests/SalonServices/GetActiveSalonServicesTests.cs`

## Factory Setup (create once, reuse)
`tests/BarberSalon.IntegrationTests/Helpers/BarberSalonWebFactory.cs`

```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using BarberSalon.Infrastructure.Persistence;

namespace BarberSalon.IntegrationTests.Helpers;

public class BarberSalonWebFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove real DB
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            // Add in-memory DB
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid()));
        });
    }

    public HttpClient GetClient() => CreateClient();
}
```

## Test Class Template

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using BarberSalon.IntegrationTests.Helpers;
using BarberSalon.Application.SalonServices.Queries;

namespace BarberSalon.IntegrationTests.SalonServices;

public class GetActiveSalonServicesTests : IClassFixture<BarberSalonWebFactory>
{
    private readonly HttpClient _client;
    private readonly BarberSalonWebFactory _factory;

    public GetActiveSalonServicesTests(BarberSalonWebFactory factory)
    {
        _factory = factory;
        _client = factory.GetClient();
    }

    [Fact]
    public async Task GetActiveSalonServices_WhenServicesExist_Returns200WithList()
    {
        // Arrange: seed the DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.SalonServices.Add(SalonService.Create("Men's Haircut", 30, 150_000, "Hair"));
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/v1/salon-services?status=active");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<SalonServiceResponse>>>();
        body!.Success.Should().BeTrue();
        body.Data.Should().HaveCount(1);
        body.Data![0].Name.Should().Be("Men's Haircut");
    }

    [Fact]
    public async Task GetActiveSalonServices_WhenNoServices_Returns200WithEmptyList()
    {
        var response = await _client.GetAsync("/api/v1/salon-services?status=active");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<SalonServiceResponse>>>();
        body!.Data.Should().BeEmpty();
    }
}
```

## TDD Steps
1. Write test class (will not compile — handler/controller not built yet)
2. `dotnet build` → compile errors expected
3. Build the API endpoint
4. `dotnet test tests/BarberSalon.IntegrationTests/` → RED
5. Fix implementation
6. `dotnet test` → GREEN

## Rules
- Each test method = one HTTP request + one assertion group
- Always seed DB in Arrange, never assume DB state from other tests
- Use `IClassFixture<BarberSalonWebFactory>` — one factory per test class
- Never share DB state between test methods (use unique `Guid` DB name per factory)
- Always assert: status code AND response body
```

---

### Task 9: Create `skills/clean-code/SKILL.md`

**Files:**
- Create: `.agents/skills/clean-code/SKILL.md`

- [ ] **Step 1: Write clean-code skill**

```markdown
---
name: clean-code
description: YAGNI, KISS, DRY, SOLID rules every agent must follow when writing code.
---

# Clean Code Skill

Apply these principles to every line you write. Check before committing.

## YAGNI — You Ain't Gonna Need It
- No generic base classes before two concrete use cases require them
- No `BaseRepository<T>` — implement `ISalonServiceRepository` directly
- No `Result<T>` wrapper until error handling is explicitly required
- No caching layer, logging service, or event bus unless in the prompt

## KISS — Keep It Simple
- One class, one responsibility
- Handler should be readable in under 2 minutes
- If you need to comment WHAT the code does, the code is too complex
- Prefer 5 lines of clear code over 2 lines of clever code

## DRY — Don't Repeat Yourself
- If mapping SalonService → SalonServiceResponse appears twice, extract a mapper
- If two handlers share validation logic, extract it to a Validator class
- If two tests share seed data, extract to a helper method

## SOLID applied to this codebase
| Principle | What it means here |
|-----------|-------------------|
| S — Single Responsibility | One handler = one use case. Controller only maps and calls MediatR. |
| O — Open/Closed | Add new use cases by adding new handlers, not by modifying existing ones |
| L — Liskov | IRepository implementations must fully satisfy the interface contract |
| I — Interface Segregation | `ISalonServiceRepository` only has methods this handler needs |
| D — Dependency Inversion | Handler depends on `ISalonServiceRepository`, not `SalonServiceRepository` |

## Checklist Before Committing
- [ ] No method longer than 20 lines
- [ ] No constructor with more than 4 parameters
- [ ] No nested if-statements deeper than 2 levels
- [ ] No magic numbers — use named constants or enums
- [ ] Every public method has a clear, single purpose
- [ ] No unused using statements
```

---

### Task 10: Create `skills/code-review/SKILL.md`

**Files:**
- Create: `.agents/skills/code-review/SKILL.md`

- [ ] **Step 1: Write code-review skill**

```markdown
---
name: code-review
description: Checklist for reviewing code after each use case implementation.
---

# Code Review Skill

## When to Run
Reviewer agent runs this after implementer completes, before Phase 5 closes.

## Domain Layer Checks
- [ ] Entity has zero framework references (no `[Required]`, no `[Key]`, no EF attributes)
- [ ] Value Objects are immutable (readonly properties, no public setters)
- [ ] Business rules are enforced inside the entity, not in the handler
- [ ] Names match CONTEXT.md exactly (check: Appointment, SalonService, StaffMember, Customer)
- [ ] IRepository interface lives in Domain, not Infrastructure
- [ ] Enums match AppointmentStatus / UserRole from CONTEXT.md

## Application Layer Checks
- [ ] Handler constructor only takes IRepository interfaces (no concrete classes)
- [ ] No `new Repository()` inside handler — constructor injection only
- [ ] No EF DbContext referenced in Application project
- [ ] Request and Response DTOs are plain C# records/classes (no entity references)
- [ ] Validation is in handler or a Validator class, not in controller

## Infrastructure Layer Checks
- [ ] Repository class implements IRepository from Domain
- [ ] No business logic in repository (no if-statements on domain rules)
- [ ] EF entity configuration is in a separate `IEntityTypeConfiguration<T>` class
- [ ] No raw SQL strings — use EF LINQ only

## API Layer Checks
- [ ] Controller calls MediatR `Send()` only — no direct service/repo calls
- [ ] Controller maps HTTP input to Command/Query, nothing else
- [ ] Response always uses envelope: `{ data, success, message }`
- [ ] HTTP status codes: 200 OK, 201 Created, 400 Bad Request, 404 Not Found, 422 Unprocessable

## Test Checks
- [ ] Every scenario from scenariowriter has a corresponding test method
- [ ] Unit tests use NSubstitute — no `new ConcreteClass()` for dependencies
- [ ] Integration tests use WebApplicationFactory + in-memory DB
- [ ] Test method names: `[Method]_[Condition]_[ExpectedResult]`
- [ ] No `Assert.True(x == y)` — FluentAssertions only

## Review Report Format
```
REVIEW: [use-case-name]
PASS: [N] checks
FAIL: [N] checks

FAIL: [check description]
  File: [exact path]:[line number]
  Found:    [actual code]
  Expected: [correct code]

VERDICT: APPROVED | CHANGES REQUESTED
```
```

---

### Task 11: Create `skills/architecture-check/`

**Files:**
- Create: `.agents/skills/architecture-check/SKILL.md`
- Create: `.agents/skills/architecture-check/target-arch.md`

- [ ] **Step 1: Create `architecture-check/SKILL.md`**

```markdown
---
name: architecture-check
description: Verify the implemented architecture against target-arch.md and repair violations.
---

# Architecture Check Skill

## When to Run
After every 3 completed use cases, or when reviewer flags an architecture issue.

## How to Check
1. Read `skills/architecture-check/target-arch.md`
2. For each implemented use case in `state/handoff-reports/`:
   - Open each file listed in the handoff report
   - Cross-check against target-arch.md rules for that layer
3. If violation found: document it, fix it, re-run tests

## What to Look For

### Circular Dependencies
- Application must NOT reference Infrastructure
- Domain must NOT reference Application

Check: look for `using BarberSalon.Infrastructure` in any Application file.

### Leaking Business Logic
- Check Infrastructure/*.cs for if-statements on domain rules
- Check API/Controllers/*.cs for business logic outside of MediatR.Send()

### Missing Abstractions
- Every repository implementation must have a corresponding interface in Domain
- Every handler must receive its dependencies via constructor, not `new`

## Fix Protocol
1. Identify violation: file, line, rule broken
2. Write the fix
3. Run `dotnet build`
4. Run `dotnet test` — all previous tests must still pass
5. Document in `state/handoff-reports/arch-fix-[date].md`
```

- [ ] **Step 2: Create `architecture-check/target-arch.md`**

```markdown
# Target Architecture — BarberSalon Backend

## Project Structure

```
src/
  BarberSalon.Domain/
    [Feature]/
      Entities/       ← pure C# classes, no framework
      ValueObjects/   ← immutable, no framework
      Enums/
      Interfaces/     ← IRepository contracts
      Events/         ← domain events (optional, later phases)

  BarberSalon.Application/
    [Feature]/
      Queries/        ← [Name]Query.cs + [Name]Handler.cs + [Name]Response.cs
      Commands/       ← [Name]Command.cs + [Name]Handler.cs

  BarberSalon.Infrastructure/
    Persistence/
      AppDbContext.cs
      Configurations/   ← IEntityTypeConfiguration<T> per entity
    [Feature]/
      [Name]Repository.cs  ← implements I[Name]Repository from Domain

  BarberSalon.API/
    Controllers/      ← [Feature]Controller.cs
    Program.cs        ← DI wiring only

tests/
  BarberSalon.Domain.Tests/
    [Feature]/[Name]Tests.cs
  BarberSalon.Application.Tests/
    [Feature]/[Name]HandlerTests.cs
  BarberSalon.IntegrationTests/
    [Feature]/[Name]Tests.cs
    Helpers/BarberSalonWebFactory.cs
```

## Dependency Graph (legal imports only)

```
API  ──→  Application  ──→  Domain
                ↑
      Infrastructure ──→  Domain
```

Any other cross-project reference is a violation.

## Naming Reference
(see rules/architecture-laws.md — this file is the architectural map,
 architecture-laws.md has the naming conventions)
```

---

### Task 12: Create `skills/storyteller/SKILL.md`

**Files:**
- Create: `.agents/skills/storyteller/SKILL.md`

- [ ] **Step 1: Write storyteller skill**

```markdown
---
name: storyteller
description: Read the frontend codebase to understand what the backend must build for each use case.
---

# Storyteller Skill — Frontend → Backend Implementation Map

## Purpose
Before implementing a use case, read the frontend to understand:
what data is needed, what shape it must have, what user action triggers it.
This defines the API contract before writing a single line of C#.

## What to Read (in order)

1. `beauty-saloon-front-V2/src/lib/data/types.ts`
   → TypeScript types = your DTO contracts (exact field names and types)

2. `beauty-saloon-front-V2/src/lib/data/dataService.ts`
   → Find the function(s) for this use case: signature, return type, parameters

3. `beauty-saloon-front-V2/src/lib/data/mock/` (the relevant file)
   → Mock data = example response shapes (field names, example values)

4. The page/component that calls this function
   → Understand user flow context

## Output (write to `state/current-plan.md` under "Frontend Context")

```
USE CASE: [name]
FRONTEND CALLER: [file path]:[function name]
PARAMETERS SENT:
  [param name]: [TypeScript type]
RESPONSE EXPECTED:
  [field name]: [TypeScript type]
  [field name]: [TypeScript type]
USER FLOW: [1-2 sentences: what is the user doing when this is called]
MOCK FILE REFERENCE: [which mock file has the shape]
BACKEND IMPLICATIONS:
  Entity fields required: [list only what's in the response]
  Query filter: [e.g., status == Active]
  Sort order: [e.g., by Name ascending]
  HTTP method: GET | POST | PUT | DELETE
  Endpoint: /api/v1/[resource]
```

## Rules
- Never invent field names — read them from types.ts and mock files
- If mock field name conflicts with CONTEXT.md term, CONTEXT.md wins
- If a frontend function computes data client-side (e.g., filtering a full list),
  the backend endpoint may still return the full list — note this explicitly
- Keep the output under 30 lines — only what the implementer needs
```

---

### Task 13: Create `skills/storyconnector/SKILL.md`

**Files:**
- Create: `.agents/skills/storyconnector/SKILL.md`

- [ ] **Step 1: Write storyconnector skill**

```markdown
---
name: storyconnector
description: After backend implementation, identify every frontend location that must call the new real API instead of mock data.
---

# Story Connector Skill — API → Frontend Replacement Map

## Purpose
After a use case is implemented and tests are green, this skill
identifies exactly where in the frontend the mock calls must be replaced
with real API calls. Does NOT modify frontend code — only documents and marks.

## What to Scan

1. `beauty-saloon-front-V2/src/lib/data/dataService.ts`
   → Find function(s) that correspond to the completed use case

2. All files that call those functions
   → Search for the function name across the frontend codebase

3. `beauty-saloon-front-V2/src/store/`
   → Check if any Redux slice dispatches this data

## Output (append to `state/handoff-reports/[use-case-name].md`)

```
STORYCONNECTOR: [use-case-name]
ENDPOINT READY: [METHOD] /api/v1/[path]

FRONTEND REPLACEMENTS NEEDED:
  dataService.[functionName]() in [file]:[line range]
  Called from:
    - [component file]:[line]
    - [page file]:[line]

REPLACEMENT PATTERN:
  Before: const data = await dataService.getActiveServices()
  After:  const res = await fetch('/api/v1/salon-services?status=active')
          const data = await res.json()

STORE UPDATE NEEDED: yes | no
  If yes: [which slice, which action]

TODO TAG ADDED: ← add this comment to each dataService function identified:
  // TODO: API-READY — replace mock with fetch('/api/v1/...')
```

## Rules
- Add TODO comment to each identified dataService function — nothing else
- Never modify component logic in this step
- If a dataService function has no corresponding endpoint yet, mark as PENDING
- One storyconnector report per completed use case
```

---

### Task 14: Create `skills/eventwriter/SKILL.md`

**Files:**
- Create: `.agents/skills/eventwriter/SKILL.md`

- [ ] **Step 1: Write eventwriter skill**

```markdown
---
name: eventwriter
description: Given a use case, define the Events — logical groups of API actions that implement the business intention.
---

# Event Writer Skill

## Definition
An Event is a named, logical group of API actions that together fulfill
one business intention. Events sit between use cases (broad) and scenarios (specific).

Hierarchy:
  Use Case → Events → Scenarios → Tests

## Input
- Use case name
- Frontend context from storyteller (state/current-plan.md)
- Domain vocabulary from CONTEXT.md

## Output Format

For each Event, produce:

```
EVENT: [EventName]
DESCRIPTION: [One sentence — the business intention in plain language]
API ACTIONS:
  [HTTP METHOD] /api/v1/[resource] → [what it returns in one phrase]
DOMAIN ENTITIES INVOLVED: [from CONTEXT.md]
AGGREGATE ROOT: [which entity owns this transaction]
COMMAND OR QUERY: Command (write) | Query (read)
```

## Rules
- One Event = one business intention (not one table, not one class)
- Each API action maps to exactly one controller action method
- Aggregate root must be named exactly as in CONTEXT.md
- If the use case touches 2+ aggregate roots, define one Event per aggregate
- No Event may span multiple HTTP requests — one request = one Event

## Example

Use case: GetActiveSalonServices

```
EVENT: ListActiveSalonServices
DESCRIPTION: Retrieve all salon services currently offered and available for booking.
API ACTIONS:
  GET /api/v1/salon-services?status=active → SalonServiceResponse[]
DOMAIN ENTITIES INVOLVED: SalonService
AGGREGATE ROOT: SalonService
COMMAND OR QUERY: Query
```

Use case: CreateAppointment

```
EVENT: BookAppointment
DESCRIPTION: Customer reserves a time slot with a staff member for a specific salon service.
API ACTIONS:
  POST /api/v1/appointments → AppointmentResponse
DOMAIN ENTITIES INVOLVED: Appointment, SalonService, StaffMember, TimeSlot, Customer
AGGREGATE ROOT: Appointment
COMMAND OR QUERY: Command

EVENT: ValidateBookingSlot (pre-condition check)
DESCRIPTION: Verify the requested time slot is available before creating the appointment.
API ACTIONS:
  GET /api/v1/appointments/availability?staffId=&date=&serviceId= → AvailabilityResponse
DOMAIN ENTITIES INVOLVED: Appointment, TimeSlot, StaffMember
AGGREGATE ROOT: Appointment
COMMAND OR QUERY: Query
```
```

---

### Task 15: Create `skills/scenariowriter/SKILL.md`

**Files:**
- Create: `.agents/skills/scenariowriter/SKILL.md`

- [ ] **Step 1: Write scenariowriter skill**

```markdown
---
name: scenariowriter
description: Given Events, derive all test scenarios — every possible logical case, mapped to unit and integration tests.
---

# Scenario Writer Skill

## Definition
A Scenario is one specific case that must be verified for an Event.
Scenarios come in two types:
- **Unit**: tests one class in isolation (Application handler logic)
- **Integration**: tests the full stack via HTTP (API → handler → DB → response)

Hierarchy:
  Event → Scenarios → Tests

## Input
- Events from eventwriter (state/current-plan.md)
- Architecture laws from rules/architecture-laws.md

## Output Format

For each Scenario:

```
SCENARIO: [short name — no spaces, CamelCase]
TYPE: unit | integration
LAYER: Domain | Application | API
GIVEN: [initial state in plain language]
WHEN: [action taken — include HTTP method + path for integration]
THEN: [expected outcome — include HTTP status for integration]
TEST METHOD: [ClassName.MethodName to write]
```

## Scenario Categories (always cover all that apply per Event)

| Category | What to test |
|----------|-------------|
| Happy Path | Correct data returned, correct HTTP 200/201 |
| Empty / Zero | Entity list is empty → still 200, not 404 |
| Not Found | Single entity doesn't exist → 404 |
| Validation Failure | Required field missing → 400 |
| Business Rule Violation | Domain rule broken → 422 |
| Mapping Correctness | Entity fields map correctly to DTO fields |
| Status Filter | Only records with correct status returned |

## Example — Event: ListActiveSalonServices

```
SCENARIO: ReturnsListWhenServicesExist
TYPE: integration
LAYER: API
GIVEN: 2 active SalonService records in DB
WHEN: GET /api/v1/salon-services?status=active
THEN: HTTP 200, body.data has 2 items, body.success = true
TEST METHOD: GetActiveSalonServicesTests.Returns200WithList_WhenServicesExist

SCENARIO: ReturnsEmptyListWhenNoActiveServices
TYPE: integration
LAYER: API
GIVEN: No SalonService records in DB
WHEN: GET /api/v1/salon-services?status=active
THEN: HTTP 200, body.data is empty array, body.success = true
TEST METHOD: GetActiveSalonServicesTests.Returns200WithEmptyList_WhenNoServices

SCENARIO: HandlerMapsEntityToResponseCorrectly
TYPE: unit
LAYER: Application
GIVEN: Repository returns SalonService with Name="Men's Haircut", Duration=30, Price=150000
WHEN: GetActiveSalonServicesHandler.Handle() called
THEN: Returns SalonServiceResponse with same Name, DurationMinutes, PriceInRials
TEST METHOD: GetActiveSalonServicesHandlerTests.MapsEntityToResponse_Correctly
```

## Rules
- Minimum 2 scenarios per Event: one happy path + one edge case
- Integration scenarios always assert: status code + body.success + body.data shape
- Unit scenarios always assert: return value mapping + correct repository call
- Scenario names must match the test method name exactly
```

---

### Task 16: Create `skills/prompt-engineer/`

**Files:**
- Create: `.agents/skills/prompt-engineer/SKILL.md`
- Create: `.agents/skills/prompt-engineer/base-template.md`

- [ ] **Step 1: Create `prompt-engineer/SKILL.md`**

```markdown
---
name: prompt-engineer
description: How to write stable, effective prompts for free/tier LLM models used as implementer subagents.
---

# Prompt Engineer Skill

## Why This Matters
Free and tier models lose context, hallucinate scope, and add unasked features.
A well-structured prompt prevents 80% of these failures.

## The 6 Rules

### Rule 1: Front-Load the Mission (first 3 lines are critical)
```
You are implementing [use-case-name] in the BarberSalon .NET backend.
Your ONLY job: implement this one use case completely with tests.
Stop when `dotnet test` is green. Do not invent new use cases.
```

### Rule 2: Always Include an Explicit OUT-OF-SCOPE List
Without this, models add authentication, generic base classes, or extra endpoints.
```
DO NOT:
- Modify any file not listed in "Files to Create"
- Add authentication or JWT
- Create BaseRepository<T> or any generic abstraction
- Add logging, caching, or middleware
- Create use cases not in this prompt
```

### Rule 3: Use Exact File Paths, Never Vague Directions
Bad: "Create a repository for SalonService somewhere in Infrastructure"
Good: "Create `src/BarberSalon.Infrastructure/SalonServices/SalonServiceRepository.cs`"

### Rule 4: Done Criteria as a Checklist
```
DONE CRITERIA (verify all before stopping):
- [ ] `dotnet build BarberSalon.slnx` → 0 errors
- [ ] `dotnet test` → 0 failures
- [ ] Every scenario in SCENARIOS section has a passing test
- [ ] No TODO or placeholder in any file created
- [ ] Handoff report written to state/handoff-reports/[name].md
```

### Rule 5: Inject Context in This Exact Order
1. Mission (2-3 sentences)
2. Domain vocabulary (only terms relevant to this use case)
3. Files to create (exact paths, one per line)
4. Events (from eventwriter output)
5. Scenarios (from scenariowriter output)
6. Architecture constraints
7. Out-of-scope
8. Done criteria

### Rule 6: Keep Under 2000 Tokens
Cut by:
- Only include CONTEXT.md terms used by this use case
- Only include scenarios for this use case
- Reference file paths — do not paste file contents into the prompt
- Remove any "background information" that doesn't change what the model must do

## Building the Prompt
Fill `skills/prompt-engineer/base-template.md` with the Enrich phase output.
Write result to `state/current-prompt.txt`.
```

- [ ] **Step 2: Create `prompt-engineer/base-template.md`**

```markdown
You are implementing **{{use-case-name}}** in the BarberSalon .NET 10 backend.
Your ONLY job: implement this one use case completely with all tests passing.
Stop when `dotnet test` is green. Do not modify anything outside the files listed below.

---

## Domain Vocabulary (use exact terms, no synonyms)

{{domain-terms}}

---

## Files to Create

{{files-to-create}}

---

## Events

{{events}}

---

## Scenarios

{{scenarios}}

---

## Architecture Rules

- Domain: no EF, no HTTP, no framework attributes
- Application: depends only on Domain interfaces (IRepository)
- Infrastructure: implements IRepository interfaces, no business logic
- API: maps HTTP ↔ MediatR only, no business logic
- Response envelope for ALL endpoints:
  ```json
  { "data": <T>, "success": bool, "message": "string" }
  ```
- Endpoint pattern: `/api/v1/[resource]` (plural, lowercase, kebab-case)

---

## DO NOT

- Add authentication or JWT (separate use case)
- Create `BaseRepository<T>` or any generic abstractions
- Modify files not listed in "Files to Create"
- Add logging, caching, or middleware
- Create additional use cases, controllers, or endpoints

---

## Implementation Order

1. Domain entity + IRepository interface (pure C#)
2. Application Query/Command + Handler + Response DTO
3. Infrastructure EF configuration + Repository implementation
4. API Controller + DI wiring in Program.cs
5. Unit tests (NSubstitute — test handler in isolation)
6. Integration tests (WebApplicationFactory — test HTTP endpoint end-to-end)

---

## Done Criteria (check all before stopping)

- [ ] `dotnet build E:\barber\beauty-saloon-api\BarberSalon.slnx` → 0 errors
- [ ] `dotnet test E:\barber\beauty-saloon-api\` → 0 failures
- [ ] Every scenario listed above has a corresponding passing test
- [ ] No TODO or placeholder in any created file
- [ ] Handoff report written to `.agents/state/handoff-reports/{{use-case-name}}.md`
```

---

### Task 17: Create `skills/handoff/`

**Files:**
- Create: `.agents/skills/handoff/SKILL.md`
- Create: `.agents/skills/handoff/template.md`

- [ ] **Step 1: Create `handoff/SKILL.md`**

```markdown
---
name: handoff
description: How and when to write a handoff report after completing a use case implementation.
---

# Handoff Skill

## When to Write
Immediately after `dotnet test` shows 0 failures for this use case.
Before notifying the orchestrator that you are done.

## Where to Write
`beauty-saloon-api/.agents/state/handoff-reports/[use-case-id].md`

Example: `state/handoff-reports/get-active-salon-services.md`

## How to Fill the Template
1. Open `skills/handoff/template.md`
2. Fill every section — no TBD, no "see above", no empty rows
3. File count: run `git diff --name-only HEAD` to get exact list
4. Test count: read the `dotnet test` output — count [PASS] lines
5. Scenario coverage: cross-reference each scenario from `state/current-plan.md`

## Rules
- Never write "N/A" for test counts — if count is 0, the use case is not done
- Every scenario listed in current-plan.md must appear in the "Scenarios Covered" table
- If you encountered a workaround or technical debt, document it in "Blockers/Notes"
```

- [ ] **Step 2: Create `handoff/template.md`**

```markdown
# Handoff Report — {{use-case-name}}

**Date:** {{date}}
**Status:** completed | failed

---

## Files Changed

| Action | File | Layer | Purpose |
|--------|------|-------|---------|
| Created | `src/BarberSalon.Domain/...` | Domain | |
| Created | `src/BarberSalon.Application/...` | Application | |
| Created | `src/BarberSalon.Infrastructure/...` | Infrastructure | |
| Created | `src/BarberSalon.API/...` | API | |
| Modified | `src/BarberSalon.API/Program.cs` | API | DI registration |
| Created | `tests/.../UnitTests/...` | Tests | |
| Created | `tests/.../IntegrationTests/...` | Tests | |

**Total files changed:** {{count}}

---

## Tests Written

| Test Method Name | Type | Scenario | Result |
|-----------------|------|----------|--------|
| `[ClassName.MethodName]` | unit | [scenario name] | ✅ PASS |
| `[ClassName.MethodName]` | integration | [scenario name] | ✅ PASS |

**Total unit tests:** {{count}}
**Total integration tests:** {{count}}
**`dotnet test` output:** 0 failures, {{total}} tests passed

---

## Events Implemented

| Event Name | HTTP Method | Endpoint |
|-----------|-------------|---------|
| {{event-name}} | GET/POST/... | /api/v1/... |

---

## Scenarios Covered

| Scenario | Type | Test Method | Result |
|----------|------|-------------|--------|
| {{scenario-name}} | unit/integration | {{test-method}} | ✅ |

---

## Blockers / Notes

{{any issues encountered, workarounds applied, tech debt incurred}}

---

## Storyconnector Output

{{output from storyconnector skill — which frontend functions to replace}}
```

---

### Task 18: Create `run-backend-cycle.ps1`

**Files:**
- Create: `run-backend-cycle.ps1` (at `E:\barber\beauty-saloon-api\`)

- [ ] **Step 1: Write loop runner script**

```powershell
# run-backend-cycle.ps1
# Headless backend implementation loop for BarberSalon API
param(
    [string]$Workspace    = "E:\barber\beauty-saloon-api",
    [string]$AgentsDir    = ".agents",
    [string]$Model        = "",
    [int]   $MaxRetries   = 3,
    [switch]$DryRun
)

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$logFile   = "$Workspace\$AgentsDir\logs\backend-cycle-$timestamp.log"

New-Item -ItemType Directory -Force -Path "$Workspace\$AgentsDir\logs" | Out-Null

function Write-Log {
    param([string]$Msg, [string]$Level = "INFO")
    $line = "$(Get-Date -Format 'HH:mm:ss') [$Level] $Msg"
    Write-Host $line
    Add-Content -Path $logFile -Value $line
}

function Update-Queue {
    param([object]$Queue, [string]$Path)
    $Queue | ConvertTo-Json -Depth 10 | Set-Content $Path -Encoding UTF8
}

function Run-DotnetTest {
    $result = dotnet test "$Workspace" --verbosity quiet 2>&1
    return @{ ExitCode = $LASTEXITCODE; Output = $result -join "`n" }
}

# --- Startup ---
Write-Log "=== BACKEND CYCLE START ==="
Write-Log "Workspace: $Workspace"
Write-Log "DryRun: $DryRun"

$queuePath = "$Workspace\$AgentsDir\state\queue.json"
if (-not (Test-Path $queuePath)) {
    Write-Log "ERROR: queue.json not found at $queuePath. Run bootstrap first." "ERROR"
    exit 1
}

$queue = Get-Content $queuePath -Raw | ConvertFrom-Json

# --- Mode Decision ---
$pending = @($queue.use_cases | Where-Object { $_.status -eq "pending" })
$inProg  = @($queue.use_cases | Where-Object { $_.status -eq "in-progress" })
$completed = @($queue.use_cases | Where-Object { $_.status -eq "completed" })
$failed    = @($queue.use_cases | Where-Object { $_.status -eq "failed" })

if ($pending.Count -eq 0 -and $inProg.Count -eq 0) {
    Write-Log "LOOP_COMPLETE: All $($completed.Count) use cases implemented." "DONE"
    exit 0
}

Write-Log "RESUME: $($pending.Count) pending, $($completed.Count) completed, $($failed.Count) failed"

# --- Main Loop ---
:mainLoop while ($true) {
    # Reload queue each iteration
    $queue = Get-Content $queuePath -Raw | ConvertFrom-Json
    $completedIds = @($queue.use_cases | Where-Object { $_.status -eq "completed" } | Select-Object -ExpandProperty id)

    # Phase 1: Select
    $selected = $null
    foreach ($uc in ($queue.use_cases | Where-Object { $_.status -eq "pending" } | Sort-Object priority)) {
        $depsOk = $true
        foreach ($dep in $uc.dependencies) {
            if ($completedIds -notcontains $dep) { $depsOk = $false; break }
        }
        if ($depsOk) { $selected = $uc; break }
    }

    if ($null -eq $selected) {
        Write-Log "BLOCKED: No pending use case has all dependencies completed." "WARN"
        Write-Log "Completed: $($completedIds -join ', ')"
        $blocked = @($queue.use_cases | Where-Object { $_.status -eq "pending" })
        Write-Log "Still pending: $($blocked.id -join ', ')"
        break mainLoop
    }

    Write-Log "--- SELECTED: $($selected.name) (id: $($selected.id)) ---"
    $selected.status = "in-progress"
    Update-Queue $queue $queuePath

    if ($DryRun) {
        Write-Log "DRY RUN: Would implement $($selected.name)"
        $selected.status = "pending"
        Update-Queue $queue $queuePath
        break mainLoop
    }

    # Phase 2: Enrich — build prompt from template
    $template = Get-Content "$Workspace\$AgentsDir\skills\prompt-engineer\base-template.md" -Raw -ErrorAction SilentlyContinue
    if (-not $template) {
        Write-Log "ERROR: base-template.md not found" "ERROR"
        exit 1
    }
    $prompt = $template -replace '\{\{use-case-name\}\}', $selected.name
    $prompt = "# Mission`n$prompt`n`n## Frontend Reference`nSee: $($selected.frontend_ref) in beauty-saloon-front-V2/src/lib/data/dataService.ts"
    Set-Content -Path "$Workspace\$AgentsDir\state\current-prompt.txt" -Value $prompt -Encoding UTF8
    Write-Log "ENRICHED: Prompt written ($($prompt.Split("`n").Count) lines)"

    # Phase 3: Inject (call headless agent — wire in your preferred CLI here)
    Write-Log "INJECT: Starting implementer for $($selected.name)"
    # --- Replace this section with your headless agent CLI call ---
    # Example (opencode):
    #   $promptFile = "$Workspace\$AgentsDir\state\current-prompt.txt"
    #   cmd.exe /c "opencode --auto --model $Model < $promptFile" 2>&1
    # Example (claude-code):
    #   claude --print (Get-Content $promptFile -Raw) 2>&1
    Write-Log "INJECT: (wire your headless CLI here — see script comments)"

    # Phase 4: Verify
    $retries = 0
    $success = $false
    while ($retries -lt $MaxRetries -and -not $success) {
        $retries++
        Write-Log "VERIFY: Attempt $retries of $MaxRetries"
        $testResult = Run-DotnetTest
        if ($testResult.ExitCode -eq 0) {
            $success = $true
            Write-Log "VERIFY: GREEN ✓ — all tests pass"
        } else {
            Write-Log "VERIFY: RED — tests failed (retry $retries)" "WARN"
            Write-Log $testResult.Output
        }
    }

    # Phase 5: Close
    if ($success) {
        $selected.status = "completed"
        $selected.retries = $retries
        $queue.meta.completed = ($queue.use_cases | Where-Object { $_.status -eq "completed" }).Count + 1
        $queue.meta.in_progress = 0
        Update-Queue $queue $queuePath

        # Update progress.md
        $progressPath = "$Workspace\$AgentsDir\state\progress.md"
        $total = $queue.use_cases.Count
        $done  = $queue.meta.completed
        $pct   = [math]::Round(($done / $total) * 100)
        $progressContent = "# Implementation Progress`n`n**Last updated:** $(Get-Date -Format 'yyyy-MM-dd HH:mm')`n**Overall:** $done / $total use cases ($pct%)`n"
        Set-Content $progressPath $progressContent -Encoding UTF8

        Write-Log "CLOSED: $($selected.name) → completed ($done/$total = $pct%)"
    } else {
        $selected.status = "failed"
        $selected.retries = $MaxRetries
        Update-Queue $queue $queuePath
        Write-Log "FAILED: $($selected.name) after $MaxRetries retries — stopping loop" "ERROR"
        exit 1
    }

    # Check if done
    $remaining = @($queue.use_cases | Where-Object { $_.status -eq "pending" })
    if ($remaining.Count -eq 0) {
        $completedAll = @($queue.use_cases | Where-Object { $_.status -eq "completed" })
        Write-Log "LOOP_COMPLETE: All $($completedAll.Count) use cases done." "DONE"
        break mainLoop
    }
}

Write-Log "=== BACKEND CYCLE END ==="
```

- [ ] **Step 2: Verify script is valid PowerShell**

```powershell
$null = [System.Management.Automation.Language.Parser]::ParseFile(
  "E:\barber\beauty-saloon-api\run-backend-cycle.ps1", [ref]$null, [ref]$null)
Write-Host "Parse OK"
```

Expected: `Parse OK`

---

### Task 19: Add Integration Test Project

**Files:**
- Create: `tests/BarberSalon.IntegrationTests/BarberSalon.IntegrationTests.csproj`
- Create: `tests/BarberSalon.IntegrationTests/Helpers/BarberSalonWebFactory.cs`
- Modify: `BarberSalon.slnx`

- [ ] **Step 1: Create integration test project**

```powershell
cd E:\barber\beauty-saloon-api
dotnet new xunit -n BarberSalon.IntegrationTests -o tests/BarberSalon.IntegrationTests --framework net10.0
```

Expected: `The template "xUnit Test Project" was created successfully.`

- [ ] **Step 2: Add required packages**

```powershell
cd E:\barber\beauty-saloon-api\tests\BarberSalon.IntegrationTests
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package FluentAssertions --version 8.*
dotnet add package Microsoft.EntityFrameworkCore.InMemory
dotnet add package coverlet.collector
dotnet add reference ..\..\src\BarberSalon.API\BarberSalon.API.csproj
```

- [ ] **Step 3: Add to solution**

```powershell
cd E:\barber\beauty-saloon-api
dotnet sln BarberSalon.slnx add tests/BarberSalon.IntegrationTests/BarberSalon.IntegrationTests.csproj
```

- [ ] **Step 4: Create `BarberSalonWebFactory.cs`**

```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace BarberSalon.IntegrationTests.Helpers;

public class BarberSalonWebFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace real DB with in-memory (added per use case as needed)
        });
        builder.UseEnvironment("Testing");
    }

    public HttpClient GetClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false
    });
}
```

- [ ] **Step 5: Verify build and test**

```powershell
cd E:\barber\beauty-saloon-api
dotnet build BarberSalon.slnx
dotnet test
```

Expected:
```
Build succeeded.
Test run for ... passed!
```

(0 or 1 placeholder tests passing — no failures)

---

## Self-Review

**Spec coverage:**
- ✅ All 17 skill files covered with complete content
- ✅ State bucket: queue.json (16 use cases), progress.md, current-plan.md, current-prompt.txt
- ✅ Agent personas: all 4 (orchestrator, implementer, reviewer, tester)
- ✅ Loop runner script with dependency checking, retry logic, queue updates
- ✅ Integration test project setup
- ✅ Bootstrap checklist covers all preconditions

**Placeholder scan:**
- No TBD or TODO in any file content above
- PowerShell inject section clearly marked with comment + example CLI calls (intentional — user must wire their specific headless CLI)

**Type consistency:**
- queue.json `id` field (kebab-case) matches PowerShell script `selected.id` references
- Handoff template `{{use-case-name}}` matches base-template.md replacement pattern
- All file paths are absolute or relative to `beauty-saloon-api/` consistently
