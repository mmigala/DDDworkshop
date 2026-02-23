# DDD vs Non-DDD Workshop

A hands-on ASP.NET workshop comparing **Domain-Driven Design** with a traditional **service-blob / anemic-model** approach, using a realistic **rights-based licensing** domain from the Digital Asset Management (DAM) world.

Both projects expose **identical API endpoints** and produce the same results — the difference is entirely in how the code is organized, where business rules live, and what that means for testability and maintainability.

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

## Test Testability Comparison

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
