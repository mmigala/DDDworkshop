# DDD vs Non-DDD Workshop – Requirements

## Goal
Prepare a sample ASP.NET project demonstrating the differences between DDD (Domain-Driven Design) and Non-DDD approaches to organizing domain logic.

## Requirements

### DDD Project
- Showcase **Aggregates** – clearly defined aggregate roots with consistency boundaries
- Showcase **Entities** – objects with identity and lifecycle
- Showcase **Value Objects** – immutable objects defined by their attributes, not identity
- Showcase **Domain Isolation** – domain layer has no dependencies on infrastructure/application concerns
- Demonstrate rich domain models with behavior encapsulated inside domain objects
- Proper use of repositories abstracting persistence
- Domain events (if applicable)

### Non-DDD Project
- Same functionality implemented without DDD patterns
- Most logic lives in **services** (anemic domain model)
- Models are plain data containers (DTOs / anemic entities)
- Show how this approach leads to:
  - Business logic scattered across services
  - Harder to maintain invariants
  - Tight coupling to infrastructure
  - Less discoverability of domain rules

### Comparison Points
- Code organization and structure
- Where business rules live
- Testability
- Maintainability and readability
- Enforcing invariants and consistency

---

## Domain: Rights-Based Licensing for Assets

### Core User Story
A customer wants to use an asset (photo/video) in a certain way (channel, territory, time period, purpose). The system must decide:
- **Allowed / Not allowed**
- If allowed: issue a **license grant** (an auditable "permission document")
- Track **revocations** and **expiry**
- Support **exclusive rights** (conflicts)

### Ubiquitous Language

| Term | Meaning |
|------|---------|
| **Asset** | The media item being licensed |
| **Rights Profile** | The legal constraints attached to an asset (who owns it, restrictions) |
| **License Terms** | What usage is requested (territory, channel, time window, purpose) |
| **License Grant** | The issued permission (allowed usage + audit trail) |
| **Exclusivity** | Only one active grant for a given scope |

---

## DDD Design: Aggregates, Entities, Value Objects, Policies

### Aggregate 1: `AssetRights` (Aggregate Root)
Represents the rights rules attached to an asset.

**Why it's an aggregate:** Most invariants are "about rights for a single asset", and conflicts typically occur within that boundary.

#### Entities inside
- `RightRestriction` (e.g., "no political ads", "editorial only", "no print", etc.)
- `ExclusiveWindow` (if the asset is exclusively licensed somewhere for a period)

#### Value Objects
- `AssetId`
- `OwnerId` / `LicensorId`
- `UsageChannel` (Web, Print, Social, TV…)
- `Territory` (ISO country codes / region sets)
- `TimeWindow` (start/end with rules)
- `UsagePurpose` (Editorial, Commercial, Internal, Political…)
- `LicenseScope` (Channel + Territory + TimeWindow + Purpose)
- `Money` (if you want price calculation later)

#### Key Behaviors (methods on aggregate root)
- `Evaluate(requestedTerms)` → `RightsDecision` (Allowed/Denied + reasons)
- `ReserveExclusiveScope(grantId, scope)` (if you model exclusivity reservation)
- `RevokeExclusiveScope(grantId)`
- `AddRestriction(...)` / `RemoveRestriction(...)` (admin actions)

#### Invariants
- You cannot add overlapping `ExclusiveWindow` entries that conflict.
- A request is denied if it violates restrictions (purpose/channel/territory/time).
- Time window must be valid (start < end, not exceeding max duration if policy says so).

> **This shows aggregate invariants + encapsulation.**

---

### Aggregate 2: `LicenseGrant` (Aggregate Root)
Represents an issued license (auditable document).

#### Entities inside
- `GrantStatusHistory` (optional: issued → active → expired → revoked)

#### Value Objects
- `LicenseGrantId`
- `AssetId`
- `LicenseeId` (customer)
- `LicenseTerms` (the granted terms)
- `IssuedAt`, `ExpiresAt`
- `RevocationReason`

#### Key Behaviors
- `Issue(...)`
- `Revoke(reason, byUser)`
- `Expire(now)` (or computed)
- `IsActive(now)`

#### Invariants
- Cannot revoke an already expired grant (or you can allow but track state rules).
- Cannot "issue" if already issued.
- Terms are immutable once issued (change = revoke + issue new revision).

> **This shows entity lifecycle + state transitions + auditability.**

---

### Domain Policy / Domain Service: `ExclusiveLicensingPolicy`
Exclusivity often requires checking existing grants (cross-aggregate).

**This is a great place to show:**
- **Domain service:** business logic that doesn't belong to a single entity.
- **Isolation:** aggregate doesn't query the DB itself.

#### Responsibilities
- When issuing a license with `IsExclusive = true`, ensure no other active grants overlap in scope.
- Uses repositories:
  - `ILicenseGrantRepository.FindActiveByAsset(assetId)`
  - `IAssetRightsRepository`

---

### Domain Events
When a grant is issued: `LicenseGranted(AssetId, GrantId, Terms, LicenseeId)`

**Handlers (application layer / integration):**
- Update search index
- Notify downstream system
- Create watermarking job
- Log for compliance

> **This shows decoupling + eventual consistency.**

---

## DDD Project Structure

```
DDDworkshop.Dam.Rights.Domain/
DDDworkshop.Dam.Rights.Application/
DDDworkshop.Dam.Rights.Infrastructure/
DDDworkshop.Dam.Rights.Api/
DDDworkshop.Dam.Rights.Tests/          (pure domain tests + app tests)
```

### Domain Layer contains
- Aggregates, entities, value objects (immutable)
- Domain events
- Domain exceptions (e.g., `RightsViolationException`)
- Policies / domain services interfaces (or implementations if they're pure)

### Application Layer contains
- Commands/queries: `RequestLicenseCommand`, `RevokeLicenseCommand`
- Handlers (thin orchestration)
- DTO mapping (outward)
- Interfaces: repositories, clock, unit of work

### Infrastructure
- In-memory repositories (backed by `ConcurrentDictionary`)
- In-process domain event dispatcher
- `IClock` implementation
- No EF Core, no database, no external dependencies — everything runs in-memory

### API
- Controllers + request/response models
- Authentication/authorization integration

---

## Non-DDD "Service Blob" Project

### Typical Structure
```
Entities/Asset.cs          (mutable)
Entities/License.cs        (mutable)
RightsService.cs           (200–800 lines)
LicenseService.cs          (another 400 lines)
Controllers call services which mutate EF entities directly
```

### What Goes Wrong (demonstrable problems)

| Problem | Description |
|---------|-------------|
| **Invariants not protected** | One developer updates `LicenseEntity.ExpiresAt` directly "to fix data"; another forgets to check exclusivity in a new endpoint |
| **Rules duplicated** | "Editorial only" checked in multiple services; "Territory overlap" implemented 2–3 times inconsistently |
| **No isolation** | Many classes can mutate the same entity graph; hard to reason about "where business rules live" |
| **Testing gets heavy** | Need EF Core setup to test rules (or lots of mocking); hard to write small pure tests for "territory overlap" etc. |
| **Harder refactoring** | Change "territory" from string to structured set? Touches everywhere. |

---

## Concrete API Endpoints (same for both projects)

### Licensing
| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/assets/{assetId}/license-requests` | Body: licenseeId, channel, territory, timeWindow, purpose, exclusive? → returns allowed/denied + grantId |
| `POST` | `/license-grants/{grantId}/revoke` | Revoke a grant |
| `GET` | `/license-grants/{grantId}` | Get grant details |
| `GET` | `/assets/{assetId}/license-grants?activeOnly=true` | List grants for asset |

### Rights Admin
| Method | Endpoint | Description |
|--------|----------|-------------|
| `PUT` | `/assets/{assetId}/rights-profile` | Set owner/licensor, base restrictions |
| `POST` | `/assets/{assetId}/rights-profile/restrictions` | Add restriction |
| `POST` | `/assets/{assetId}/rights-profile/exclusive-windows` | Add exclusive window |

---

## Demo Scenarios

### Scenario A: Allowed editorial use
- Asset is "Editorial only"
- Request: Editorial + Web + Norway + 3 months → **Allowed** → Grant issued

### Scenario B: Denied commercial use
- Request: Commercial → **Denied** with reason `PurposeNotAllowed`

### Scenario C: Exclusivity conflict
- Existing active grant: exclusive Web+NO in March
- New request: exclusive Web+NO overlapping March → **Denied** `ExclusiveConflict`

### Scenario D: Revocation & audit
- Revoke grant with reason (contract breach)
- Grant transitions to `Revoked`, events fired

**These scenarios map to:**
- Value objects (`Territory`, `TimeWindow`)
- Policies (`ExclusiveLicensingPolicy`)
- Aggregates and state transitions (`LicenseGrant.Revoke()`)

---

## Key DDD Concepts Visibly Shown

| Concept | Example |
|---------|---------|
| **Aggregate root** | `AssetRights`, `LicenseGrant` |
| **Entities** | `ExclusiveWindow`, `Restriction`, `GrantHistoryEntry` |
| **Value objects** | `Territory`, `TimeWindow`, `LicenseTerms`, `UsageChannel`, `Purpose` |
| **Isolation/encapsulation** | No public setters; only methods enforce transitions |
| **Domain services/policies** | Exclusivity checks across grants |
| **Domain events** | `LicenseGranted`, `LicenseRevoked` |
| **Boundaries** | Rights decisions vs downstream processes (watermarking/indexing) |

---

## Extra DAM-Realistic Rule (Optional)

**Model releases required for commercial use.**
- Value object: `ReleaseStatus` (None / ModelRelease / PropertyRelease / Both)
- Restriction: "Commercial requires ModelRelease"

> Adds depth without blowing up the scope.
