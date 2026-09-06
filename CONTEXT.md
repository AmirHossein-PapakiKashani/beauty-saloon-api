# CONTEXT.md — BarberSalon Domain Glossary

> **Single source of truth for domain vocabulary.**
> This file is a glossary — nothing else.
> No implementation details. No architecture decisions. No specs.
> When a term is used in code, docs, or conversation, it MUST match this definition.

---

## Core Terms

### Appointment
A confirmed reservation created when a **Customer** books a **SalonService** with a specific **StaffMember** at a specific **TimeSlot**.
An Appointment is the central entity of the business — everything else revolves around it.

**Not the same as:** Booking (the act), Reservation (not used in this codebase).
**Code name:** `Appointment` (entity in `Domain/Booking/Entities/`).

---

### Booking
The *act* of a Customer reserving a SalonService. Booking is a process, not a stored entity.
When booking completes, it produces an **Appointment**.

**Not the same as:** Appointment (the result).
**Code name:** `BookingService` (application service); `Booking` (module folder name).

---

### Customer
A person who visits the salon to receive services. Has a profile with contact details, loyalty history, and beauty notes.
A Customer is created when a User with role `Customer` completes registration.

**Not the same as:** User (the auth identity), Client (not used).
**Code name:** `Customer` (entity in `Domain/Customers/Entities/`).

---

### User
The authentication identity. Every person who logs in has a User record.
A User has one Role: `Customer`, `Staff`, or `Admin`.
A User is linked 1-to-1 with a Customer or StaffMember record after first login.

**Not the same as:** Customer (the salon-domain person), Account (not used).
**Code name:** `User` (entity in `Domain/Auth/Entities/`).

---

### StaffMember
An employee of the salon who performs services. Has a schedule, set of offered services, and performance record.

**Not the same as:** User (the auth identity — a StaffMember has a linked User).
**Code name:** `StaffMember` (entity). In UI and conversation: "Staff" or "Stylist" is acceptable shorthand.
**Never use in code:** `Employee`, `Provider`, `Therapist`.

---

### SalonService
A type of service offered by the salon — e.g. "Men's Haircut", "Beard Trim", "Hair Colour".
Defines name, duration, price, and category. Not the same as a DDD domain service.

**Critical disambiguation:** The word "service" is overloaded.
  - `SalonService` = a salon offering (business entity, stored in DB).
  - "domain service" = a DDD pattern (not used in this project).
  - "application service" = a class in the Application layer (e.g. `BookingService`).
When writing code or docs, always use the full term `SalonService` for the business entity.

**Code name:** `SalonService` (entity in `Domain/SalonServices/Entities/`).
**Never use in code:** `Treatment`, `Offering`, `Product`.

---

### TimeSlot
An immutable Value Object representing a specific block of time: `Date`, `StartTime`, `EndTime`.
Two TimeSlots overlap if their intervals intersect on the same date for the same StaffMember.

**Code name:** `TimeSlot` (value object in `Domain/Booking/ValueObjects/`).
**Never use in code:** `Slot`, `Block`, `Interval`, `Window`.

---

### AppointmentStatus
The lifecycle state of an Appointment. Transitions are enforced inside the `Appointment` entity.

| Status | Meaning |
|--------|---------|
| `Pending` | Created; awaiting confirmation |
| `Confirmed` | Confirmed by staff or admin |
| `Completed` | Service was delivered |
| `Cancelled` | Cancelled by customer or admin |
| `NoShow` | Customer did not arrive |

Valid transitions:
- `Pending` → `Confirmed`, `Cancelled`
- `Confirmed` → `Completed`, `Cancelled`, `NoShow`
- `Completed` → (terminal — no further transitions)
- `Cancelled` → (terminal)
- `NoShow` → (terminal)

**Code name:** `AppointmentStatus` (enum in `Domain/Booking/Enums/`).

---

### UserRole
The access level of a User. Determines what the user can see and do.

| Role | Description |
|------|-------------|
| `Customer` | Books appointments, views own profile |
| `Staff` | Views assigned appointments, updates status |
| `Admin` | Full access: CRM, services, staff, analytics |

**Code name:** `UserRole` (enum in `Domain/Auth/Enums/`).

---

### LoyaltyAccount
A per-Customer record tracking loyalty points and tier.
Points are earned on completed Appointments and can be redeemed for discounts.

**Code name:** `LoyaltyAccount` (entity in `Domain/Loyalty/Entities/`).
**Never use in code:** `RewardAccount`, `PointsCard`.

---

### WaitlistEntry
A request by a Customer to be notified when a desired SalonService + StaffMember + date becomes available due to a cancellation.

**Code name:** `WaitlistEntry` (entity in `Domain/Waitlist/Entities/`).
**Never use in code:** `QueueEntry`, `HoldRequest`.

---

### Review
A rating and optional comment left by a Customer after a completed Appointment.
A Review is linked to exactly one Appointment.

**Code name:** `Review` (entity in `Domain/Reviews/Entities/`).
**Never use in code:** `Feedback`, `Rating` (Rating is a property of Review, not the entity itself).

---

### OTP (One-Time Password)
A short numeric code (5 digits) sent via SMS to a phone number for authentication.
Valid for 5 minutes. A new OTP request invalidates the previous one for the same number.

**Code name:** `OtpService` (infrastructure service); `OtpCodes` (database table).

---

### Archive / Archived
Soft-delete pattern used throughout. An archived entity has `IsActive = false` and is hidden from normal listings but not deleted from the database.
Applies to: `SalonService`, `StaffMember`, `Customer`.

**Not the same as:** Delete (hard delete — never used), Deactivate (same concept, but "Archive" is the canonical term here).
**Code name:** `Archive()` method on entities; `IsActive` property.

---

## Term Disambiguation Table

| Ambiguous word | What it means in THIS codebase |
|---------------|-------------------------------|
| "service" | Depends on context — always clarify:<br>→ Business offering = `SalonService`<br>→ Application logic class = `*Service` (e.g. `BookingService`)<br>→ DDD domain service = not used |
| "staff" / "stylist" | Acceptable in conversation; in code always `StaffMember` |
| "cancel" | Transition to `AppointmentStatus.Cancelled` via `Appointment.Cancel()` |
| "delete" | Does not exist — use Archive (`IsActive = false`) |
| "account" | Refers to `LoyaltyAccount`, NOT to User or Customer |
| "slot" | Always `TimeSlot` in code |
| "book" / "reserve" | The act = Booking; the result = Appointment |

---

## Out of Scope (not modelled)

These concepts are deliberately absent from the domain model:

| Concept | Reason |
|---------|--------|
| Payment / Invoice | Phase 2 — not yet modelled |
| Inventory / Products | Out of scope for current SaaS |
| Multi-branch | Single-salon only |
| Expense / P&L | Out of scope |
| Commission | Out of scope for current phase |
