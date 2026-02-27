# DDD vs Non-DDD Workshop

A hands-on ASP.NET workshop comparing **Domain-Driven Design** with a traditional **service-blob / anemic-model** approach, using a realistic **rights-based licensing** domain from the Digital Asset Management (DAM) world.

Both projects expose **identical API endpoints** and produce the same results — the difference is entirely in how the code is organized, where business rules live, and what that means for testability and maintainability.

---

## Table of Contents

- [DDD vs Non-DDD Workshop](#ddd-vs-non-ddd-workshop)
  - [Table of Contents](#table-of-contents)
  - [What Does This App Do?](#what-does-this-app-do)
    - [The Business Problem](#the-business-problem)
    - [How It Works](#how-it-works)
    - [Real-World Analogy](#real-world-analogy)
  - [Quick Start](#quick-start)
    - [Prerequisites](#prerequisites)
    - [Run the DDD API (port 5177)](#run-the-ddd-api-port-5177)
    - [Run the Non-DDD API (port 5185)](#run-the-non-ddd-api-port-5185)
    - [Run Tests](#run-tests)
  - [Solution Structure](#solution-structure)
  - [Side-by-Side Comparison](#side-by-side-comparison)
  - [Key DDD Concepts Demonstrated](#key-ddd-concepts-demonstrated)
  - [Demo Scenarios](#demo-scenarios)
    - [Scenario A: Allowed Editorial Use](#scenario-a-allowed-editorial-use)
    - [Scenario B: Denied Commercial Use](#scenario-b-denied-commercial-use)
    - [Scenario C: Exclusivity Conflict](#scenario-c-exclusivity-conflict)
    - [Scenario D: Revocation \& Audit](#scenario-d-revocation--audit)
  - [Anti-Patterns Highlighted in Non-DDD Project](#anti-patterns-highlighted-in-non-ddd-project)
  - [Common DDD Anti-Patterns (and How to Avoid Them)](#common-ddd-anti-patterns-and-how-to-avoid-them)
  - [Test Testability Comparison](#test-testability-comparison)
    - [DDD Tests (61 tests — pure, fast, no infrastructure)](#ddd-tests-61-tests--pure-fast-no-infrastructure)
    - [Non-DDD Tests (17 tests — heavy setup, fragile)](#non-ddd-tests-17-tests--heavy-setup-fragile)
  - [API Endpoints (Same for Both Projects)](#api-endpoints-same-for-both-projects)
  - [Tech Stack](#tech-stack)
  - [DDD Concepts Glossary](#ddd-concepts-glossary)

---

## What Does This App Do?

This application models a **rights-based licensing system** for digital assets (photos, videos, illustrations) — the kind of system used by stock media companies, news agencies, and content marketplaces.

### The Business Problem

A media company owns a library of digital assets. Customers want to **use** those assets — on a website, in a TV broadcast, in a print magazine, etc. But usage isn't unlimited. Each asset has legal constraints:

- **Who owns it** and what releases are in place (model release, property release)
- **Where** it can be used (specific countries/territories)
- **How** it can be used (web, print, broadcast, social media)
- **Why** it's being used (editorial reporting vs. commercial advertising)
- **When** it can be used (specific time windows)
- **Exclusively or not** (can multiple customers use it at the same time?)

### How It Works

1. **Set up an asset's rights profile** — define the owner, release status, and any restrictions (e.g., "no commercial use", "no broadcast in the US")
2. **Request a license** — a customer asks to use the asset for a specific channel, territory, time period, and purpose
3. **The system evaluates the request** — checks all restrictions, exclusivity conflicts, and release requirements
4. **Grant or deny** — if allowed, a license grant is issued (an auditable permission record); if not, the system returns the specific reasons why
5. **Manage grants** — view active licenses, revoke them if needed (e.g., contract breach), track status history

### Real-World Analogy

Think of it like a **concert venue booking system**: the venue (asset) has rules about what events are allowed, a promoter (licensee) requests a date and type of event, the system checks for conflicts and restrictions, and if approved, a booking contract (license grant) is issued. Exclusive bookings block others from using the same slot.

---

## Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (net10.0)
- Any HTTP client: VS Code REST Client extension, Postman, curl, or the built-in `.http` files

### Run the DDD API (port 5177)

```bash
dotnet run --project DDDworkshop.Dam.Rights.Api
```

Swagger UI: [http://localhost:5177/swagger](http://localhost:5177/swagger)

### Run the Non-DDD API (port 5185)

```bash
dotnet run --project DDDworkshop.Dam.NoDdd.Api
```

Swagger UI: [http://localhost:5185/swagger](http://localhost:5185/swagger)

### Run Tests

```bash
dotnet test DDDworkshop.Dam.Rights.Tests
```

78 tests: DDD domain (pure), DDD application (stub repos), Non-DDD contrast (highlighting anti-patterns).

---

## Solution Structure

<details>
<summary>Click to expand project tree</summary>

```
DDDworkshop.slnx
│
├── DDDworkshop.Dam.Rights.Domain/          # Domain layer (zero dependencies)
│   ├── SeedWork/                           # Base classes: ValueObject, Entity<T>, AggregateRoot<T>, IDomainEvent
│   ├── ValueObjects/                       # AssetId, Territory, TimeWindow, LicenseScope, LicenseTerms, ...
│   ├── Aggregates/
│   │   ├── AssetRightsAggregate/           # AssetRights (root), RightRestriction, ExclusiveWindow, RightsDecision
│   │   └── LicenseGrantAggregate/          # LicenseGrant (root), GrantStatus, GrantStatusHistoryEntry
│   ├── Policies/                           # ExclusiveLicensingPolicy (domain service)
│   ├── Events/                             # LicenseGrantedEvent, LicenseRevokedEvent
│   ├── Repositories/                       # IAssetRightsRepository, ILicenseGrantRepository (interfaces only)
│   └── Exceptions/                         # DomainException, RightsViolationException, InvalidTimeWindowException
│
├── DDDworkshop.Dam.Rights.Application/     # Application layer (depends only on Domain)
│   ├── Commands/                           # RequestLicenseCommand, RevokeLicenseCommand, ...
│   ├── Handlers/                           # Thin orchestrators: load aggregate → call domain → persist
│   ├── Dtos/                               # RightsDecisionDto, LicenseGrantDto, AssetRightsProfileDto
│   ├── Mapping/                            # DtoMapper (domain → DTO)
│   └── Abstractions/                       # IClock, IDomainEventDispatcher
│
├── DDDworkshop.Dam.Rights.Infrastructure/  # Infrastructure (depends on Application)
│   ├── Repositories/                       # InMemory*Repository (ConcurrentDictionary)
│   ├── Services/                           # SystemClock, InProcessDomainEventDispatcher
│   └── EventHandlers/                      # LicenseGrantedEventHandler, LicenseRevokedEventHandler
│
├── DDDworkshop.Dam.Rights.Api/             # DDD API (ASP.NET Core Web API)
│   ├── Controllers/                        # Thin controllers → command → handler → response
│   ├── Models/Requests & Responses/        # Separate API models (decoupled from domain)
│   ├── Mapping/                            # ResponseMapper (DTO → API response)
│   └── Middleware/                         # DomainExceptionMiddleware
│
├── DDDworkshop.Dam.NoDdd.Api/              # Non-DDD API (single project, same endpoints)
│   ├── Entities/                           # Mutable data bags (public setters, raw strings)
│   ├── Data/                               # InMemoryDataStore (flat ConcurrentDictionaries)
│   ├── Services/                           # RightsService (~270 lines), LicenseService (~260 lines)
│   └── Controllers/                        # Controllers call services directly, return raw data
│
└── DDDworkshop.Dam.Rights.Tests/           # Tests (xUnit)
    ├── Domain/ValueObjects/                # TimeWindowTests, TerritoryTests (pure, zero infra)
    ├── Domain/Aggregates/                  # AssetRightsTests, LicenseGrantTests (pure)
    ├── Domain/Policies/                    # ExclusiveLicensingPolicyTests (stub repo)
    ├── Application/                        # RequestLicenseHandlerTests (stub everything)
    └── NoDddContrast/                      # RightsServiceContrastTests, LicenseServiceContrastTests
```

</details>

---

## Side-by-Side Comparison

| Aspect | DDD Project | Non-DDD Project |
|--------|-------------|-----------------|
| **Where rules live** | In aggregates and value objects (`AssetRights.Evaluate()`, `Territory.OverlapsWith()`) | In service classes (`RightsService`, `LicenseService`) — entities are data bags |
| **Type safety** | Enums (`UsageChannel`, `UsagePurpose`), value objects (`Territory` with ISO validation) | Raw strings everywhere — `"Wbe"` typo compiles and silently fails |
| **Encapsulation** | Private setters, factory methods, state-transition guards in the aggregate | Public setters — `grant.Status = "Banana"` compiles and runs |
| **Invariants** | Enforced at aggregate boundary — impossible to create invalid state | Guards only in services — any code with entity access can bypass them |
| **Duplication** | `RightRestriction.Blocks()` is one testable method | `RestrictionBlocks()` duplicated in both `RightsService` AND `LicenseService` |
| **Territory overlap** | `Territory.OverlapsWith()` — pure value object, 3-line test | Inline `Split(",")` + `Intersect()` in 3+ places, untestable in isolation |
| **Domain events** | `LicenseGrantedEvent` raised by aggregate, dispatched via `IDomainEventDispatcher` | No events — downstream effects must be manually added to every call site |
| **Audit trail** | `GrantStatusHistoryEntry` list, immutable, built into aggregate lifecycle | None — status is just overwritten, no transition history |
| **Testability** | Pure domain tests (no infra), stub repos for app layer — **61 DDD tests** | Requires full `InMemoryDataStore` + service chain for any test — **17 contrast tests** |
| **Dependencies** | Domain layer has **zero** NuGet/project dependencies | Everything in one project — services depend on `InMemoryDataStore` directly |
| **Layer count** | 4 layers (Domain → Application → Infrastructure → API) | 1 flat project |
| **DI registrations** | Grouped by layer with interfaces | Flat list, concrete classes, no abstractions |

---

## Key DDD Concepts Demonstrated

| Concept | Where to Find It |
|---------|-----------------|
| **Value Object** | `Territory`, `TimeWindow`, `LicenseScope`, `LicenseTerms`, `AssetId`, `LicenseGrantId` |
| **Entity** | `RightRestriction` (has identity + `Blocks()` method), `ExclusiveWindow`, `GrantStatusHistoryEntry` |
| **Aggregate Root** | `AssetRights` (rights + restrictions + exclusive windows), `LicenseGrant` (lifecycle + audit) |
| **Factory Method** | `LicenseGrant.Issue()` — only way to create a grant, ensures consistent initial state |
| **Domain Service** | `ExclusiveLicensingPolicy` — cross-aggregate logic (checks grants for scope overlap) |
| **Domain Event** | `LicenseGrantedEvent`, `LicenseRevokedEvent` — raised by aggregate, dispatched after persist |
| **Repository** | `IAssetRightsRepository`, `ILicenseGrantRepository` — interfaces in Domain, impls in Infrastructure |
| **Specification / Policy Pattern** | `RightRestriction.Blocks()` — encapsulates restriction matching as a single testable method |
| **Encapsulation** | No public setters on aggregates; all state changes go through methods with guard clauses |
| **Isolation** | Domain layer has no dependencies; Application layer only references Domain |

---

## Demo Scenarios

Use the `.http` files in VS Code (REST Client extension) or Swagger UI to walk through these scenarios. Run both APIs side-by-side to compare behavior.

<details>
<summary>Click to expand all scenarios</summary>

### Scenario A: Allowed Editorial Use

> An asset has editorial restrictions. A request for editorial use on the Web in Norway for 3 months should be **allowed** and a grant issued.

**Steps:**
1. Create a rights profile (PUT `/assets/{assetId}/rights-profile`)
2. Add restriction: block commercial use (POST `.../restrictions`)
3. Request editorial license (POST `/assets/{assetId}/license-requests`)
4. Result: `isAllowed: true`, `grantId` returned

**DDD flow:** Controller → `RequestLicenseCommand` → `RequestLicenseHandler` → `AssetRights.Evaluate()` → `LicenseGrant.Issue()` → `LicenseGrantedEvent` dispatched

**Non-DDD flow:** Controller → `LicenseService.IssueLicense()` (inline restriction check + entity creation)

### Scenario B: Denied Commercial Use

> Same asset as Scenario A. A commercial license request should be **denied** because of the restriction.

**Steps:**
1. (Asset setup from Scenario A)
2. Request commercial license (POST with `purpose: "Commercial"`)
3. Result: `isAllowed: false`, `denialReasons: ["Editorial use only - commercial blocked"]`

**DDD:** `RightRestriction.Blocks()` matches the purpose → `RightsDecision.Denied()` returned  
**Non-DDD:** `RestrictionBlocks()` private method in service (same logic, but duplicated in `LicenseService`)

### Scenario C: Exclusivity Conflict

> Issue an exclusive editorial license for Web+NO in March–June. A second exclusive request for the same scope should be **denied** with `ExclusiveConflict`.

**Steps:**
1. (Asset setup from Scenario A)
2. Request exclusive license → **allowed**, grant + exclusive window created
3. Request another exclusive license for same scope → **denied** `ExclusiveConflict`

**DDD flow:**
- First: `ExclusiveLicensingPolicy.CheckAsync()` → no conflict → `LicenseGrant.Issue()` → `AssetRights.ReserveExclusiveScope()`
- Second: `AssetRights.Evaluate()` detects `ExclusiveWindow` overlap → denied

**Non-DDD flow:** `LicenseService.IssueLicense()` inline checks both `ExclusiveWindows` dict AND active grants (two separate code paths)

### Scenario D: Revocation & Audit

> Revoke a grant with reason "Contract breach". The grant transitions to `Revoked`, a `LicenseRevokedEvent` is raised, and the status history shows the full audit trail.

**Steps:**
1. (Issue a grant from Scenario A)
2. Revoke the grant (POST `/license-grants/{grantId}/revoke`)
3. Get the grant (GET `/license-grants/{grantId}`) — status is `Revoked`

**DDD:** `LicenseGrant.Revoke()` enforces guards (can't revoke expired/already revoked), records `GrantStatusHistoryEntry`, raises `LicenseRevokedEvent`

**Non-DDD:** `LicenseService.RevokeLicense()` has guards in the service only — entity has public setters so `grant.Status = "Revoked"` works anywhere

</details>

---

## Anti-Patterns Highlighted in Non-DDD Project

Every anti-pattern in the Non-DDD project is marked with `// ⚠️ ANTI-PATTERN:` or `// ⚠️` comments explaining what's wrong and how DDD solves it. Key examples:

| Anti-Pattern | File | What Happens |
|-------------|------|-------------|
| **Anemic Entity** | `Entities/*.cs` | All entities have public setters, no behavior — just data bags |
| **Service Blob** | `RightsService.cs` (~270 lines) | ALL rights logic in one class, growing unboundedly |
| **Duplicated Rules** | `LicenseService.RestrictionBlocks()` | Same restriction-matching logic copied from `RightsService` |
| **String Typing** | `Territory` as `"NO,SE,DK"` | No ISO validation, comma-splitting scattered across methods |
| **No Events** | `LicenseService.IssueLicense()` | Side effects must be manually added to every call site |
| **No Audit Trail** | `grant.Status = "Revoked"` | Status is overwritten — no history of transitions |
| **Bypass Guards** | `grant.Status = "Banana"` | Public setter allows invalid state with no error |

---

## Common DDD Anti-Patterns (and How to Avoid Them)

DDD solves many problems, but it introduces its own pitfalls. Watch out for these:

| Anti-Pattern | What goes wrong | How to avoid it |
|-------------|----------------|-----------------|
| **God Aggregate** | The aggregate root grows into a massive class with dozens of methods, hundreds of lines, and too many responsibilities. Happens when you put everything "about an asset" into one aggregate. | Split into **separate aggregates** with clear boundaries. In this project, `AssetRights` (rules) and `LicenseGrant` (issued permissions) are deliberately separate — not one giant `Asset` aggregate. |
| **Aggregate too large / too many entities** | Loading the aggregate pulls in thousands of child entities (e.g., all grants ever issued), causing performance issues. | Keep aggregates small. Reference other aggregates **by ID**, not by direct object reference. `AssetRights` doesn't hold `LicenseGrant` objects — it only holds `ExclusiveWindow`s (a small set). |
| **Anemic Domain Model (in disguise)** | You create aggregate classes but put all logic in application handlers or domain services. The aggregate is just a data container with extra steps. | Logic belongs **inside** the aggregate. `AssetRights.Evaluate()` makes the decision, not the handler. The handler just orchestrates (load → call → save). |
| **Over-engineering value objects** | Creating a value object for every single field (`FirstName`, `LastName`, `Email`, `Description`...) when a plain string would do. Adds ceremony without business value. | Use value objects when there's **real domain logic** to encapsulate: `Territory` validates ISO codes, `TimeWindow` enforces start < end. Don't wrap strings that have no invariants. |
| **Cross-aggregate transactions** | Trying to update `AssetRights` and `LicenseGrant` in a single atomic operation. Leads to distributed locking, performance issues, and tight coupling. | Use **domain events** for cross-aggregate side effects. In this project, `LicenseGrant.Issue()` raises `LicenseGrantedEvent` → handler calls `AssetRights.ReserveExclusiveScope()` separately. |
| **Domain service does everything** | Moving logic out of aggregates and into domain services "for convenience." The service becomes the new god class. | Domain services are for logic that **genuinely spans multiple aggregates**. `ExclusiveLicensingPolicy` checks existing grants (different aggregate) — that's a valid use. Single-aggregate rules stay in the aggregate. |
| **Leaking domain types to the API** | Returning domain entities/value objects directly from controllers. API contract becomes coupled to domain model changes. | Use **DTOs** at the application layer and **response models** at the API layer. In this project: Domain → `DtoMapper` → DTO → `ResponseMapper` → API response. |
| **Repository doing business logic** | Putting query filters, calculations, or rule checks inside repository implementations. | Repositories should only **load and save** aggregates. Business decisions happen in the domain. `ILicenseGrantRepository.FindActiveByAssetAsync()` is a query — the *policy* decides what to do with the results. |

---

## Test Testability Comparison

<details>
<summary>Click to expand code examples</summary>

### DDD Tests (61 tests — pure, fast, no infrastructure)

```csharp
// Value object test — zero setup
[Fact]
public void OverlapsWith_SharedCodes_ReturnsTrue()
{
    var scandinavia = new Territory(["NO", "SE", "DK"]);
    var nordic = new Territory(["NO", "FI", "IS"]);
    Assert.True(scandinavia.OverlapsWith(nordic));
}

// Aggregate test — create object, call method, assert
[Fact]
public void Evaluate_PurposeRestricted_ReturnsDenied()
{
    var rights = new AssetRights(assetId, ownerId);
    rights.AddRestriction("No commercial", restrictedPurpose: UsagePurpose.Commercial);
    var decision = rights.Evaluate(commercialTerms);
    Assert.False(decision.IsAllowed);
}
```

### Non-DDD Tests (17 tests — heavy setup, fragile)

```csharp
// Same test — but needs full data store + service
[Fact]
public void EvaluateRights_ChannelRestricted_ReturnsDenied()
{
    var store = new InMemoryDataStore();           // ⚠️ infrastructure
    var service = new RightsService(store);         // ⚠️ full service
    var assetId = Guid.NewGuid();
    service.SetRightsProfile(assetId, ownerId, "None");  // ⚠️ pre-populate
    service.AddRestriction(assetId, "No print", "Print", null, null, null); // ⚠️ raw strings
    var (isAllowed, reasons) = service.EvaluateRights(
        assetId, "Print", "NO", start, end, "Editorial", false); // ⚠️ 7 params
    Assert.False(isAllowed);
}
```

</details>

---

## API Endpoints (Same for Both Projects)

| Method | Endpoint | Description |
|--------|----------|-------------|
| `PUT` | `/assets/{assetId}/rights-profile` | Create/update rights profile |
| `GET` | `/assets/{assetId}/rights-profile` | Get rights profile |
| `POST` | `/assets/{assetId}/rights-profile/restrictions` | Add a restriction |
| `POST` | `/assets/{assetId}/rights-profile/exclusive-windows` | Add an exclusive window |
| `POST` | `/assets/{assetId}/license-requests` | Evaluate & issue license |
| `POST` | `/license-grants/{grantId}/revoke` | Revoke a grant |
| `GET` | `/license-grants/{grantId}` | Get grant details |
| `GET` | `/assets/{assetId}/license-grants?activeOnly=true` | List grants for asset |

---

## Tech Stack

- **.NET 10** (net10.0)
- **ASP.NET Core Web API** (controllers)
- **xUnit 2.9.3** for tests
- **In-memory persistence** (ConcurrentDictionary) — no EF Core, no database
- **Swashbuckle** for Swagger UI
- No external dependencies in the Domain layer

---

## DDD Concepts Glossary

| Concept | What it is | Example in this project |
|---------|-----------|------------------------|
| **Value Object** | Immutable object defined by its attributes, no identity. Two instances with the same data are equal. | `Territory`, `TimeWindow`, `LicenseScope` |
| **Entity** | Object with a unique identity that persists over time. | `RightRestriction`, `ExclusiveWindow` |
| **Aggregate Root** | Entry point to a cluster of related entities/value objects. Enforces all invariants for the group. | `AssetRights`, `LicenseGrant` |
| **Aggregate** | The cluster itself — root + its children. Nothing outside touches the children directly. | `AssetRights` + its `RightRestriction`s + `ExclusiveWindow`s |
| **Domain Event** | A record that something meaningful happened in the domain. | `LicenseGrantedEvent`, `LicenseRevokedEvent` |
| **Domain Service / Policy** | Logic that doesn't naturally belong to a single aggregate (cross-aggregate rules). | `ExclusiveLicensingPolicy` (checks grants across aggregates) |
| **Repository** | Abstraction for loading/saving aggregates. Interface in domain, implementation in infrastructure. | `IAssetRightsRepository`, `ILicenseGrantRepository` |
| **Invariant** | A business rule that must always be true. The aggregate enforces it. | "No overlapping exclusive windows", "Cannot revoke an expired grant" |
| **Ubiquitous Language** | Shared vocabulary between developers and domain experts, reflected directly in code. | `LicenseGrant`, `RightsDecision`, `RevocationReason` — not `DataRow`, `Result`, `StatusString` |
