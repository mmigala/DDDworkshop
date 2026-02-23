namespace DDDworkshop.Dam.Rights.Tests.Domain.Policies;

using DDDworkshop.Dam.Rights.Domain.Aggregates.LicenseGrantAggregate;
using DDDworkshop.Dam.Rights.Domain.Policies;
using DDDworkshop.Dam.Rights.Domain.Repositories;
using DDDworkshop.Dam.Rights.Domain.ValueObjects;

/// <summary>
/// Tests for the ExclusiveLicensingPolicy domain service.
/// 
/// DDD BENEFIT: The policy depends on ILicenseGrantRepository (an interface),
/// so we can test it with a simple stub — no real database, no heavy setup.
/// The policy itself is pure domain logic.
/// 
/// In the Non-DDD project, exclusivity checking is scattered across
/// RightsService.EvaluateRights AND LicenseService.IssueLicense, both of
/// which need the full InMemoryDataStore to test.
/// </summary>
public class ExclusiveLicensingPolicyTests
{
    private static readonly AssetId TestAssetId = new(Guid.NewGuid());
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CheckAsync_NoActiveGrants_ReturnsNoConflict()
    {
        var repo = new StubGrantRepository([]);
        var policy = new ExclusiveLicensingPolicy(repo);
        var scope = MakeScope(UsageChannel.Web, ["NO"], UsagePurpose.Editorial);

        var result = await policy.CheckAsync(TestAssetId, scope, Now);

        Assert.False(result.HasConflict);
    }

    [Fact]
    public async Task CheckAsync_NonExclusiveActiveGrant_ReturnsNoConflict()
    {
        var grant = LicenseGrant.Issue(
            TestAssetId,
            new LicenseeId(Guid.NewGuid()),
            new LicenseTerms(MakeScope(UsageChannel.Web, ["NO"], UsagePurpose.Editorial), isExclusive: false),
            new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero));

        var repo = new StubGrantRepository([grant]);
        var policy = new ExclusiveLicensingPolicy(repo);
        var scope = MakeScope(UsageChannel.Web, ["NO"], UsagePurpose.Editorial);

        var result = await policy.CheckAsync(TestAssetId, scope, Now);

        Assert.False(result.HasConflict);
    }

    [Fact]
    public async Task CheckAsync_ExclusiveGrantOverlappingScope_ReturnsConflict()
    {
        var grant = LicenseGrant.Issue(
            TestAssetId,
            new LicenseeId(Guid.NewGuid()),
            new LicenseTerms(MakeScope(UsageChannel.Web, ["NO"], UsagePurpose.Editorial), isExclusive: true),
            new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero));

        var repo = new StubGrantRepository([grant]);
        var policy = new ExclusiveLicensingPolicy(repo);
        var scope = MakeScope(UsageChannel.Web, ["NO"], UsagePurpose.Editorial);

        var result = await policy.CheckAsync(TestAssetId, scope, Now);

        Assert.True(result.HasConflict);
        Assert.Contains("ExclusiveConflict", result.ConflictDescription);
    }

    [Fact]
    public async Task CheckAsync_ExclusiveGrantDifferentChannel_ReturnsNoConflict()
    {
        var grant = LicenseGrant.Issue(
            TestAssetId,
            new LicenseeId(Guid.NewGuid()),
            new LicenseTerms(MakeScope(UsageChannel.Web, ["NO"], UsagePurpose.Editorial), isExclusive: true),
            new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero));

        var repo = new StubGrantRepository([grant]);
        var policy = new ExclusiveLicensingPolicy(repo);
        var scope = MakeScope(UsageChannel.Print, ["NO"], UsagePurpose.Editorial);

        var result = await policy.CheckAsync(TestAssetId, scope, Now);

        Assert.False(result.HasConflict);
    }

    [Fact]
    public async Task CheckAsync_ExclusiveGrantDifferentTerritory_ReturnsNoConflict()
    {
        var grant = LicenseGrant.Issue(
            TestAssetId,
            new LicenseeId(Guid.NewGuid()),
            new LicenseTerms(MakeScope(UsageChannel.Web, ["NO"], UsagePurpose.Editorial), isExclusive: true),
            new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero));

        var repo = new StubGrantRepository([grant]);
        var policy = new ExclusiveLicensingPolicy(repo);
        var scope = MakeScope(UsageChannel.Web, ["SE"], UsagePurpose.Editorial);

        var result = await policy.CheckAsync(TestAssetId, scope, Now);

        Assert.False(result.HasConflict);
    }

    // ──────────────────────────────────────────────
    //  Helpers
    // ──────────────────────────────────────────────

    private static LicenseScope MakeScope(UsageChannel channel, string[] codes, UsagePurpose purpose)
    {
        var territory = new Territory(codes);
        var timeWindow = new TimeWindow(
            new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));
        return new LicenseScope(channel, territory, timeWindow, purpose);
    }

    /// <summary>
    /// Simple stub repository — no framework needed!
    /// DDD BENEFIT: Repository is an interface, so we can provide a trivial test double.
    /// </summary>
    private sealed class StubGrantRepository : ILicenseGrantRepository
    {
        private readonly IReadOnlyList<LicenseGrant> _grants;

        public StubGrantRepository(IReadOnlyList<LicenseGrant> grants) => _grants = grants;

        public Task<LicenseGrant?> GetByIdAsync(LicenseGrantId grantId, CancellationToken ct = default)
            => Task.FromResult(_grants.FirstOrDefault(g => g.Id.Equals(grantId)));

        public Task<IReadOnlyList<LicenseGrant>> FindByAssetAsync(AssetId assetId, bool activeOnly, DateTimeOffset now, CancellationToken ct = default)
            => Task.FromResult(_grants);

        public Task<IReadOnlyList<LicenseGrant>> FindActiveByAssetAsync(AssetId assetId, DateTimeOffset now, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<LicenseGrant>>(_grants.Where(g => g.IsActive(now)).ToList());

        public Task SaveAsync(LicenseGrant grant, CancellationToken ct = default)
            => Task.CompletedTask;
    }
}
