# Phased Implementation Plan

> Based on requirements in [todo.md](todo.md)

---

## Phase 1 – Solution Scaffolding & Value Objects

**Goal:** Set up the solution structure and implement the foundational building blocks.

### Tasks

- [x] Create solution file `DDDworkshop.slnx` (update existing) with project references
- [x] Scaffold DDD projects:
  - `DDDworkshop.Dam.Rights.Domain`
  - `DDDworkshop.Dam.Rights.Application`
  - `DDDworkshop.Dam.Rights.Infrastructure`
  - `DDDworkshop.Dam.Rights.Api`
  - `DDDworkshop.Dam.Rights.Tests`
- [x] Scaffold Non-DDD project:
  - `DDDworkshop.Dam.NoDdd.Api` (single project, all-in-one)
- [x] Implement **value objects** in Domain layer:
  - `AssetId`, `LicenseGrantId`, `LicenseeId`, `OwnerId`
  - `UsageChannel` (enum/smart enum)
  - `UsagePurpose` (enum/smart enum)
  - `Territory` (ISO country code set)
  - `TimeWindow` (start/end with validation: start < end)
  - `LicenseScope` (Channel + Territory + TimeWindow + Purpose)
  - `LicenseTerms` (scope + exclusive flag)
  - `RevocationReason`
  - `ReleaseStatus` (None / ModelRelease / PropertyRelease / Both)
- [x] Add base classes: `Entity<TId>`, `AggregateRoot<TId>`, `ValueObject`
- [x] Add domain exceptions: `RightsViolationException`, `InvalidTimeWindowException`

### DDD Concepts Introduced
Immutable value objects, strong typing for IDs, base building blocks.

---

## Phase 2 – Aggregates & Domain Logic

**Goal:** Implement the two aggregate roots with their full business rules.

### Tasks

- [x] Implement `AssetRights` aggregate root:
  - Entity: `RightRestriction` (id, restricted channel/purpose/territory)
  - Entity: `ExclusiveWindow` (id, grantId, scope, time window)
  - `AddRestriction(...)` / `RemoveRestriction(...)`
  - `Evaluate(requestedTerms)` → `RightsDecision` (Allowed/Denied + reasons)
  - `ReserveExclusiveScope(grantId, scope)`
  - `RevokeExclusiveScope(grantId)`
  - Invariant: no overlapping exclusive windows
  - Invariant: restrictions block matching requests
  - Invariant: time window validity
- [x] Implement `LicenseGrant` aggregate root:
  - Entity: `GrantStatusHistory` (status transitions with timestamps)
  - Factory method `Issue(...)` → creates grant in `Issued` state
  - `Revoke(reason, byUser)` → state transition with guard
  - `IsActive(now)` → computed from status + expiry
  - Invariant: cannot revoke expired grant
  - Invariant: cannot issue if already issued
  - Invariant: terms immutable once issued
- [x] Implement `RightsDecision` result type (Allowed/Denied + denial reasons)
- [x] Add `ReleaseStatus` restriction: "Commercial requires ModelRelease"

### DDD Concepts Introduced
Aggregate roots, entities within aggregates, encapsulation (no public setters), invariant enforcement, rich domain model.

---

## Phase 3 – Domain Services, Policies & Events

**Goal:** Add cross-aggregate logic, domain events, and policy objects.

### Tasks

- [x] Implement `ExclusiveLicensingPolicy` domain service:
  - Check existing active grants for scope overlap
  - Interface: `IExclusiveLicensingPolicy`
  - Depends on `ILicenseGrantRepository` (interface only in domain)
- [x] Define domain events:
  - `LicenseGrantedEvent` (AssetId, GrantId, Terms, LicenseeId)
  - `LicenseRevokedEvent` (GrantId, Reason, RevokedBy)
- [x] Raise events from aggregate methods (`Issue`, `Revoke`)
- [x] Define repository interfaces in domain:
  - `IAssetRightsRepository`
  - `ILicenseGrantRepository` (including `FindActiveByAsset`)

### DDD Concepts Introduced
Domain services/policies, domain events, repository abstractions, isolation (domain has zero infrastructure dependencies).

---

## Phase 4 – Application Layer (Commands, Handlers, DTOs)

**Goal:** Thin orchestration layer that coordinates domain objects.

### Tasks

- [x] Implement commands:
  - `RequestLicenseCommand` (assetId, licenseeId, channel, territory, timeWindow, purpose, exclusive)
  - `RevokeLicenseCommand` (grantId, reason, revokedBy)
  - `SetRightsProfileCommand` (assetId, ownerId, licensorId)
  - `AddRestrictionCommand` (assetId, restrictedChannel, restrictedPurpose, restrictedTerritory)
  - `AddExclusiveWindowCommand` (assetId, scope, timeWindow)
- [x] Implement command handlers:
  - `RequestLicenseHandler` – loads aggregate, evaluates, checks exclusivity policy, issues grant
  - `RevokeLicenseHandler` – loads grant, calls `Revoke()`
  - `SetRightsProfileHandler`, `AddRestrictionHandler`, `AddExclusiveWindowHandler`
- [x] Implement queries / read DTOs:
  - `LicenseGrantDto`
  - `RightsDecisionDto`
  - `AssetRightsProfileDto`
- [x] Define `IClock` and `IUnitOfWork` interfaces
- [x] Wire up domain event dispatching (in-process)

### DDD Concepts Introduced
CQRS-lite (commands vs queries), thin application layer, orchestration without business logic.

---

## Phase 5 – Infrastructure (In-Memory, No External Dependencies)

**Goal:** Implement persistence and dispatching entirely in-memory — no databases, no EF Core, no external services.

### Tasks

- [ ] Implement in-memory repositories (backed by `ConcurrentDictionary`):
  - `InMemoryAssetRightsRepository : IAssetRightsRepository`
  - `InMemoryLicenseGrantRepository : ILicenseGrantRepository`
- [ ] Implement `SystemClock : IClock`
- [ ] Implement simple domain event dispatcher:
  - Aggregates collect events in `List<IDomainEvent>`
  - Dispatcher iterates and publishes after "save" (in-process, synchronous)
  - Event handlers registered via DI
- [ ] No EF Core, no migrations, no connection strings, no outbox

### Why In-Memory?
- Keeps the workshop focused on **DDD patterns**, not ORM plumbing
- Zero setup to run — just `dotnet run`
- Data resets on restart (perfect for demos)
- Easy to swap for real persistence later (repositories are behind interfaces)

---

## Phase 6 – DDD API Layer

**Goal:** Expose the domain via REST endpoints.

### Tasks

- [ ] `LicenseRequestsController`:
  - `POST /assets/{assetId}/license-requests` → `RequestLicenseCommand`
- [ ] `LicenseGrantsController`:
  - `POST /license-grants/{grantId}/revoke` → `RevokeLicenseCommand`
  - `GET /license-grants/{grantId}`
  - `GET /assets/{assetId}/license-grants?activeOnly=true`
- [ ] `RightsProfileController`:
  - `PUT /assets/{assetId}/rights-profile` → `SetRightsProfileCommand`
  - `POST /assets/{assetId}/rights-profile/restrictions` → `AddRestrictionCommand`
  - `POST /assets/{assetId}/rights-profile/exclusive-windows` → `AddExclusiveWindowCommand`
- [ ] Request/response models (separate from domain)
- [ ] Register DI services in `Program.cs`
- [ ] Add Swagger/OpenAPI

---

## Phase 7 – Non-DDD Project (The "Service Blob")

**Goal:** Implement the same functionality without DDD, showing the contrast.

### Tasks

- [ ] Create `DDDworkshop.Dam.NoDdd.Api` project (single project)
- [ ] EF entities (mutable, public setters):
  - `AssetEntity` (Id, OwnerId, LicensorId)
  - `RestrictionEntity` (Id, AssetId, Channel, Purpose, Territory)
  - `ExclusiveWindowEntity` (Id, AssetId, GrantId, Channel, Territory, Start, End)
  - `LicenseGrantEntity` (Id, AssetId, LicenseeId, Channel, Territory, Purpose, Start, End, Exclusive, Status, IssuedAt, ExpiresAt, RevokedAt, RevocationReason)
- [ ] `RightsService.cs` – large service with all rights logic:
  - `EvaluateRights(...)` – checks restrictions, exclusivity, time windows
  - `SetRightsProfile(...)`, `AddRestriction(...)`, `AddExclusiveWindow(...)`
  - Duplicated validation scattered through methods
- [ ] `LicenseService.cs` – grant management:
  - `IssueLicense(...)` – creates entity, saves directly
  - `RevokeLicense(...)` – loads entity, mutates fields, saves
  - `GetGrant(...)`, `GetGrantsForAsset(...)`
  - Some exclusivity checks duplicated from RightsService
- [ ] Same API endpoints (controllers call services directly)
- [ ] Same in-memory storage (but entities are the "domain model", mutated directly by services)
- [ ] Add inline comments marking anti-patterns:
  - `// ⚠️ No encapsulation: any code can change Status directly`
  - `// ⚠️ Business rule duplicated from RightsService`
  - `// ⚠️ Hard to test without full EF setup`
  - etc.

### Anti-Patterns to Highlight
- Public setters on everything
- Business rules in services, not in the model
- No consistency boundary (any service can mutate any entity)
- Territory as raw `string` (no validation at type level)
- Exclusivity check duplicated in 2 places
- Testing requires mocking the data store or spinning up the full service layer

---

## Phase 8 – Tests

**Goal:** Show the testability difference between DDD and non-DDD.

### Tasks

- [ ] **DDD domain tests** (pure, no infrastructure):
  - `AssetRightsTests` – evaluate allowed/denied scenarios
  - `LicenseGrantTests` – lifecycle transitions, guard clauses
  - `TimeWindowTests`, `TerritoryTests` – value object validation
  - `ExclusiveLicensingPolicyTests` – overlap detection
- [ ] **DDD application tests**:
  - `RequestLicenseHandlerTests` – orchestration with mocked repos
- [ ] **Non-DDD tests** (to contrast):
  - `RightsServiceTests` – requires full service setup with data store
  - `LicenseServiceTests` – same heavy setup
  - Show how the same test is harder to write and more fragile (business rules not testable in isolation)

---

## Phase 9 – Demo Scenarios & Documentation

**Goal:** Make it easy to run and understand the comparison.

### Tasks

- [ ] Create `README.md` with:
  - Project overview and how to run
  - Side-by-side comparison table (DDD vs Non-DDD)
  - Demo scenario walkthroughs (A, B, C, D from todo.md)
- [ ] Add `.http` files or Postman collection for demo scenarios
- [ ] Add code comments in DDD project explaining each DDD concept
- [ ] Final review: ensure both projects compile, run, and produce same API behavior

---

## Summary

| Phase | Deliverable | Key DDD Concepts |
|-------|-------------|-----------------|
| 1 | Solution + value objects | Value objects, strong typing, base classes |
| 2 | Aggregates + domain logic | Aggregate roots, entities, encapsulation, invariants |
| 3 | Domain services + events | Domain services, policies, events, isolation |
| 4 | Application layer | Commands, handlers, CQRS-lite, thin orchestration |
| 5 | Infrastructure | In-memory repositories, event dispatcher, clock |
| 6 | DDD API | REST endpoints, DI wiring |
| 7 | Non-DDD project | Anti-patterns, service blob, anemic model |
| 8 | Tests | Testability comparison |
| 9 | Docs & demos | Scenarios, README, runnable examples |
