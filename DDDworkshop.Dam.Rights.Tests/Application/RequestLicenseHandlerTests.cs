namespace DDDworkshop.Dam.Rights.Tests.Application;

using DDDworkshop.Dam.Rights.Application.Abstractions;
using DDDworkshop.Dam.Rights.Application.Commands;
using DDDworkshop.Dam.Rights.Application.Handlers;
using DDDworkshop.Dam.Rights.Domain.Aggregates.AssetRightsAggregate;
using DDDworkshop.Dam.Rights.Domain.Aggregates.LicenseGrantAggregate;
using DDDworkshop.Dam.Rights.Domain.Policies;
using DDDworkshop.Dam.Rights.Domain.Repositories;
using DDDworkshop.Dam.Rights.Domain.SeedWork;
using DDDworkshop.Dam.Rights.Domain.ValueObjects;

/// <summary>
/// Application layer tests for the RequestLicenseHandler.
/// 
/// DDD BENEFIT: Because the handler depends on interfaces (repositories, policies, clock),
/// we can test the orchestration logic with simple stubs — no real infrastructure.
/// The handler itself is thin; the tests mainly verify that the correct domain objects
/// are called in the right order and that results are propagated properly.
/// 
/// In the Non-DDD project, testing LicenseService.IssueLicense requires the full
/// InMemoryDataStore with pre-populated entities, and there's no way to mock out
/// just the evaluation logic.
/// </summary>
public class RequestLicenseHandlerTests
{
    private static readonly Guid AssetGuid = Guid.NewGuid();
    private static readonly Guid LicenseeGuid = Guid.NewGuid();
    private static readonly Guid OwnerGuid = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_AllowedNonExclusive_ReturnsAllowedWithGrantId()
    {
        var assetRights = new AssetRights(new AssetId(AssetGuid), new OwnerId(OwnerGuid));
        var handler = CreateHandler(assetRights);
        var command = MakeCommand(isExclusive: false);

        var result = await handler.HandleAsync(command);

        Assert.True(result.IsAllowed);
        Assert.NotNull(result.GrantId);
        Assert.Empty(result.DenialReasons);
    }

    [Fact]
    public async Task HandleAsync_DeniedByRestriction_ReturnsDeniedWithReasons()
    {
        var assetRights = new AssetRights(new AssetId(AssetGuid), new OwnerId(OwnerGuid));
        assetRights.AddRestriction("No web", restrictedChannel: UsageChannel.Web);
        var handler = CreateHandler(assetRights);
        var command = MakeCommand(channel: "Web");

        var result = await handler.HandleAsync(command);

        Assert.False(result.IsAllowed);
        Assert.Null(result.GrantId);
        Assert.NotEmpty(result.DenialReasons);
    }

    [Fact]
    public async Task HandleAsync_ExclusiveDeniedByPolicy_ReturnsDenied()
    {
        var assetRights = new AssetRights(new AssetId(AssetGuid), new OwnerId(OwnerGuid));
        var conflictResult = ExclusivityCheckResult.Conflict("ExclusiveConflict: existing grant blocks scope");
        var handler = CreateHandler(assetRights, exclusivityResult: conflictResult);
        var command = MakeCommand(isExclusive: true);

        var result = await handler.HandleAsync(command);

        Assert.False(result.IsAllowed);
        Assert.Contains(result.DenialReasons, r => r.Contains("ExclusiveConflict"));
    }

    [Fact]
    public async Task HandleAsync_ExclusiveAllowed_ReservesExclusiveScope()
    {
        var assetRights = new AssetRights(new AssetId(AssetGuid), new OwnerId(OwnerGuid));
        var assetRepo = new StubAssetRightsRepository(assetRights);
        var handler = CreateHandler(assetRights, assetRepo: assetRepo);
        var command = MakeCommand(isExclusive: true);

        var result = await handler.HandleAsync(command);

        Assert.True(result.IsAllowed);
        Assert.Single(assetRights.ExclusiveWindows);
        Assert.True(assetRepo.SaveWasCalled);
    }

    [Fact]
    public async Task HandleAsync_Allowed_GrantIsSaved()
    {
        var assetRights = new AssetRights(new AssetId(AssetGuid), new OwnerId(OwnerGuid));
        var grantRepo = new StubLicenseGrantRepository();
        var handler = CreateHandler(assetRights, grantRepo: grantRepo);
        var command = MakeCommand();

        await handler.HandleAsync(command);

        Assert.NotNull(grantRepo.SavedGrant);
        Assert.Equal(new AssetId(AssetGuid), grantRepo.SavedGrant!.AssetId);
    }

    [Fact]
    public async Task HandleAsync_Allowed_DomainEventsAreDispatched()
    {
        var assetRights = new AssetRights(new AssetId(AssetGuid), new OwnerId(OwnerGuid));
        var dispatcher = new StubEventDispatcher();
        var handler = CreateHandler(assetRights, eventDispatcher: dispatcher);
        var command = MakeCommand();

        await handler.HandleAsync(command);

        Assert.NotEmpty(dispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task HandleAsync_AssetNotFound_ThrowsInvalidOperationException()
    {
        var handler = CreateHandler(assetRights: null);
        var command = MakeCommand();

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(command));
    }

    // ──────────────────────────────────────────────
    //  Helpers
    // ──────────────────────────────────────────────

    private static RequestLicenseCommand MakeCommand(
        string channel = "Web",
        string purpose = "Editorial",
        bool isExclusive = false)
    {
        return new RequestLicenseCommand(
            AssetId: AssetGuid,
            LicenseeId: LicenseeGuid,
            Channel: channel,
            TerritoryCodes: ["NO"],
            TimeWindowStart: new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero),
            TimeWindowEnd: new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            Purpose: purpose,
            IsExclusive: isExclusive);
    }

    private static RequestLicenseHandler CreateHandler(
        AssetRights? assetRights,
        StubAssetRightsRepository? assetRepo = null,
        StubLicenseGrantRepository? grantRepo = null,
        ExclusivityCheckResult? exclusivityResult = null,
        StubEventDispatcher? eventDispatcher = null)
    {
        assetRepo ??= new StubAssetRightsRepository(assetRights);
        grantRepo ??= new StubLicenseGrantRepository();
        var policy = new StubExclusivityPolicy(exclusivityResult ?? ExclusivityCheckResult.NoConflict());
        var clock = new StubClock(Now);
        eventDispatcher ??= new StubEventDispatcher();

        return new RequestLicenseHandler(assetRepo, grantRepo, policy, clock, eventDispatcher);
    }

    // ──────────────────────────────────────────────
    //  Stub Implementations (simple, no framework)
    // ──────────────────────────────────────────────

    /// <summary>
    /// DDD BENEFIT: All dependencies are interfaces → trivial test doubles.
    /// No mocking framework needed for clean, readable tests.
    /// </summary>
    private sealed class StubAssetRightsRepository : IAssetRightsRepository
    {
        private readonly AssetRights? _assetRights;
        public bool SaveWasCalled { get; private set; }

        public StubAssetRightsRepository(AssetRights? assetRights) => _assetRights = assetRights;

        public Task<AssetRights?> GetByIdAsync(AssetId assetId, CancellationToken ct = default)
            => Task.FromResult(_assetRights);

        public Task SaveAsync(AssetRights assetRights, CancellationToken ct = default)
        {
            SaveWasCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class StubLicenseGrantRepository : ILicenseGrantRepository
    {
        public LicenseGrant? SavedGrant { get; private set; }

        public Task<LicenseGrant?> GetByIdAsync(LicenseGrantId grantId, CancellationToken ct = default)
            => Task.FromResult<LicenseGrant?>(null);

        public Task<IReadOnlyList<LicenseGrant>> FindByAssetAsync(AssetId assetId, bool activeOnly, DateTimeOffset now, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<LicenseGrant>>([]);

        public Task<IReadOnlyList<LicenseGrant>> FindActiveByAssetAsync(AssetId assetId, DateTimeOffset now, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<LicenseGrant>>([]);

        public Task SaveAsync(LicenseGrant grant, CancellationToken ct = default)
        {
            SavedGrant = grant;
            return Task.CompletedTask;
        }
    }

    private sealed class StubExclusivityPolicy : IExclusiveLicensingPolicy
    {
        private readonly ExclusivityCheckResult _result;

        public StubExclusivityPolicy(ExclusivityCheckResult result) => _result = result;

        public Task<ExclusivityCheckResult> CheckAsync(AssetId assetId, LicenseScope scope, DateTimeOffset now, CancellationToken ct = default)
            => Task.FromResult(_result);
    }

    private sealed class StubClock : IClock
    {
        public DateTimeOffset UtcNow { get; }
        public StubClock(DateTimeOffset utcNow) => UtcNow = utcNow;
    }

    private sealed class StubEventDispatcher : IDomainEventDispatcher
    {
        public List<IDomainEvent> DispatchedEvents { get; } = [];

        public Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default)
        {
            DispatchedEvents.AddRange(events);
            return Task.CompletedTask;
        }
    }
}
