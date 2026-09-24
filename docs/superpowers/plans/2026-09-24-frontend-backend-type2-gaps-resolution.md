# Implementation Plan: Category 2 Gaps Resolution (Frontend & Backend Alignment)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Close all Category 2 integration gaps where backend endpoints and business rules exist but frontend UI/state wiring is incomplete, disconnected, or falls back silently to mock arrays.

**Architecture:** Next.js 16 App Router client wiring with RTK Query and Redux Toolkit. All data interactions transition from static initial state mocks to reactive query hooks (`useGetAppointmentsQuery`, `useGetCustomersQuery`, `useGetStaffQuery`, `useApplyReferralCodeMutation`, `useUpdateUserProfileMutation`, `useUpdateBeautyProfileMutation`) with deterministic fallback to local state on network isolation.

**Tech Stack:** Next.js 16 (App Router), React 19, TypeScript 5.9, Redux Toolkit Query (RTK Query), Vitest, Testing Library, ASP.NET Core 10 Web API.

**Spec:** Documented frontend-to-backend Category 2 gaps:
1. User profile completion & identity synchronization (`usersApi` in `login/page.tsx` and `profile/page.tsx`).
2. Referral application & booking business actions persistence (`loyaltyApi` in `book/page.tsx`).
3. Dynamic computation of Smart Dashboard panels (`RevenueForecast`, `ChurnAlerts`, `GapAnalysis` in `admin/dashboard/page.tsx`).
4. Live customer beauty profile & treatment history binding (`beautyProfileApi` & past appointments in `admin/customers/[id]/CustomerProfileClient.tsx`).
5. Real-time appointment status invalidation & waitlist notification trigger in booking cancellation flow.

## Global Constraints

- Never break offline / cold-start fallback (always support `liveData ?? fallbackData`).
- Never introduce new external npm packages (leverage already-installed RTK Query, clsx, Lucide).
- Maintain 100% test pass rate across both backend (403/403 tests) and frontend (889/889 tests).
- All mutations must unwrap errors and surface user-friendly feedback via styled UI states instead of browser `alert()`.
- Route links and file references must follow markdown `[Filename](file:///path)` schema.

---

### Task 1: Complete User Profile & Identity Synchronization Flow

**Files:**
- Modify: `src/app/(auth)/login/page.tsx:120-155`
- Modify: `src/store/api/usersApi.ts:20-45`
- Test: `src/test/loginLiveAuth.test.tsx`

**Interfaces:**
- Consumes: `useUpdateUserProfileMutation()` from `@/store/api/usersApi`
- Produces: Persistent name update in backend database with visual error state and auto-resume to original target URL.

- [ ] **Step 1: Write failing test in `src/test/loginLiveAuth.test.tsx`**

```tsx
it("calls updateUserProfile and handles server validation or success gracefully", async () => {
  // Assert profile completion sends PUT /api/v1/users/{id}/profile with valid payload
});
```

- [ ] **Step 2: Run test to confirm failure**

```bash
npm test -- src/test/loginLiveAuth.test.tsx
```

- [ ] **Step 3: Update `src/app/(auth)/login/page.tsx`**

Add input validation (minimum 2 characters, trimming), explicit error state display on `profile-complete` step, and loading feedback during `updateProfileMutation`.

- [ ] **Step 4: Run test to verify pass**

```bash
npm test -- src/test/loginLiveAuth.test.tsx
```

---

### Task 2: Live Referral Application in Booking Submission

**Files:**
- Modify: `src/app/(customer)/book/page.tsx:280-340`
- Test: `src/test/bookPageAppointmentCreation.test.tsx`

**Interfaces:**
- Consumes: `useApplyReferralCodeMutation()` from `@/store/api/loyaltyApi`
- Produces: Backend-persisted referral completion when booking is confirmed with a valid referral code.

- [ ] **Step 1: Write failing test in `src/test/bookPageAppointmentCreation.test.tsx`**

Verify that submitting a booking with `appliedReferral.code` triggers `applyReferralCodeMutation` to `/api/v1/loyalty/apply-referral`.

- [ ] **Step 2: Run test to confirm failure**

```bash
npm test -- src/test/bookPageAppointmentCreation.test.tsx
```

- [ ] **Step 3: Wire `useApplyReferralCodeMutation` into `BookPageContent`**

In `handleSubmit`, invoke `applyReferralMutation({ code: appliedReferral.code, customerId: user.id })` upon successful appointment creation. Replace `alert(...)` with a clean inline error state.

- [ ] **Step 4: Run test to verify pass**

```bash
npm test -- src/test/bookPageAppointmentCreation.test.tsx
```

---

### Task 3: Dynamic Computation of Smart Dashboard Panels

**Files:**
- Modify: `src/app/admin/dashboard/page.tsx:25-60`
- Modify: `src/store/slices/adminSlice.ts:270-290`
- Test: `src/test/dashboardApi.integration.test.ts`

**Interfaces:**
- Consumes: `useGetAppointmentsQuery()`, `useGetCustomersQuery()`, `useGetStaffQuery()`
- Produces: Dynamic calculation of `revenueForecast`, `atRiskCustomers`, and `gaps` based on live API entities instead of static Redux constants.

- [ ] **Step 1: Write test verifying dynamic dashboard computations**

Create unit/integration test verifying that new live appointments dynamically reflect in `revenueForecast` and `gapAnalysis`.

- [ ] **Step 2: Run test to confirm failure or need for dynamic wiring**

```bash
npm test -- src/test/dashboardApi.integration.test.ts
```

- [ ] **Step 3: Update `src/app/admin/dashboard/page.tsx`**

Import `useGetCustomersQuery` and `useGetStaffQuery`. Pass effective live entities into `computeRevenueForecast`, `computeAtRiskCustomers`, and `computeGapAnalysis` wrapped in `useMemo`.

- [ ] **Step 4: Run test to verify pass**

```bash
npm test -- src/test/dashboardApi.integration.test.ts
```

---

### Task 4: Real Treatment History & Live Beauty Profile Sync

**Files:**
- Modify: `src/app/admin/customers/[id]/CustomerProfileClient.tsx:90-150`
- Test: `src/test/beautyProfileApi.integration.test.ts`

**Interfaces:**
- Consumes: `useGetAppointmentsQuery({ customerId })`, `useUpdateBeautyProfileMutation()`
- Produces: Automatic population of customer treatment history from completed backend appointments, and live persistence of operator notes.

- [ ] **Step 1: Write failing test in `src/test/beautyProfileApi.integration.test.ts`**

Assert that completed appointments appear in beauty history and updating profile triggers `updateBeautyProfileMutation`.

- [ ] **Step 2: Run test to confirm failure**

```bash
npm test -- src/test/beautyProfileApi.integration.test.ts
```

- [ ] **Step 3: Update `CustomerProfileClient.tsx`**

Merge completed live appointments into the beauty history timeline and ensure notes updates persist to backend via `updateBeautyProfileMutation`.

- [ ] **Step 4: Run test to verify pass**

```bash
npm test -- src/test/beautyProfileApi.integration.test.ts
```

---

### Task 5: End-to-End Verification and Certification

**Files:**
- Verify: Full backend solution (`dotnet test`)
- Verify: Full frontend test suite (`npm test`)
- Verify: Production build (`npm run build`)

- [ ] **Step 1: Run all backend integration tests**

```bash
dotnet test --verbosity minimal
```

- [ ] **Step 2: Run all frontend unit and integration tests**

```bash
npm test
```

- [ ] **Step 3: Run frontend production build**

```bash
npm run build
```
