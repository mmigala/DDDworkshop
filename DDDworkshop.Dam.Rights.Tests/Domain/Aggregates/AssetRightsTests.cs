namespace DDDworkshop.Dam.Rights.Tests.Domain.Aggregates;

using DDDworkshop.Dam.Rights.Domain.Aggregates.AssetRightsAggregate;
using DDDworkshop.Dam.Rights.Domain.Exceptions;
using DDDworkshop.Dam.Rights.Domain.ValueObjects;

/// <summary>
/// Pure domain tests for the AssetRights aggregate root.
/// 
/// DDD BENEFIT: All business rules live in the aggregate and can be tested
/// purely in memory — no database, no services, no mocking framework.
/// Each test creates an aggregate, calls a method, and asserts the outcome.
/// 
/// In the Non-DDD project, you'd need to set up InMemoryDataStore + RightsService
/// to test the same rules, and the rules aren't even in the entity.
/// </summary>
public class AssetRightsTests
{
    private static readonly AssetId TestAssetId = new(Guid.NewGuid());
    private static readonly OwnerId TestOwnerId = new(Guid.NewGuid());

    // ──────────────────────────────────────────────
    //  Restriction Evaluation
    // ──────────────────────────────────────────────

    [Fact]
    public void Evaluate_NoRestrictions_ReturnsAllowed()
    {
        var rights = new AssetRights(TestAssetId, TestOwnerId, ReleaseStatus.ModelRelease);
        var terms = MakeTerms(UsageChannel.Web, ["NO"], UsagePurpose.Editorial);

        var decision = rights.Evaluate(terms);

        Assert.True(decision.IsAllowed);
        Assert.Empty(decision.DenialReasons);
    }

    [Fact]
    public void Evaluate_PurposeRestricted_ReturnsDenied()
    {
        var rights = new AssetRights(TestAssetId, TestOwnerId);
        rights.AddRestriction("No commercial use", restrictedPurpose: UsagePurpose.Commercial);

        var terms = MakeTerms(UsageChannel.Web, ["NO"], UsagePurpose.Commercial);

        var decision = rights.Evaluate(terms);

        Assert.False(decision.IsAllowed);
        Assert.Contains(decision.DenialReasons, r => r.Contains("No commercial use"));
    }

    [Fact]
    public void Evaluate_ChannelRestricted_ReturnsDenied()
    {
        var rights = new AssetRights(TestAssetId, TestOwnerId);
        rights.AddRestriction("No print", restrictedChannel: UsageChannel.Print);

        var terms = MakeTerms(UsageChannel.Print, ["NO"], UsagePurpose.Editorial);

        var decision = rights.Evaluate(terms);

        Assert.False(decision.IsAllowed);
    }

    [Fact]
    public void Evaluate_ChannelRestriction_DifferentChannel_ReturnsAllowed()
    {
        var rights = new AssetRights(TestAssetId, TestOwnerId);
        rights.AddRestriction("No print", restrictedChannel: UsageChannel.Print);

        var terms = MakeTerms(UsageChannel.Web, ["NO"], UsagePurpose.Editorial);

        var decision = rights.Evaluate(terms);

        Assert.True(decision.IsAllowed);
    }

    [Fact]
    public void Evaluate_TerritoryRestricted_OverlappingTerritory_ReturnsDenied()
    {
        var rights = new AssetRights(TestAssetId, TestOwnerId);
        rights.AddRestriction("No US distribution",
            restrictedChannel: UsageChannel.Web,
            restrictedTerritory: new Territory(["US"]));

        var terms = MakeTerms(UsageChannel.Web, ["US", "NO"], UsagePurpose.Editorial);

        var decision = rights.Evaluate(terms);

        Assert.False(decision.IsAllowed);
    }

    [Fact]
    public void Evaluate_TerritoryRestricted_NonOverlapping_ReturnsAllowed()
    {
        var rights = new AssetRights(TestAssetId, TestOwnerId);
        rights.AddRestriction("No US distribution",
            restrictedChannel: UsageChannel.Web,
            restrictedTerritory: new Territory(["US"]));

        var terms = MakeTerms(UsageChannel.Web, ["NO"], UsagePurpose.Editorial);

        var decision = rights.Evaluate(terms);

        Assert.True(decision.IsAllowed);
    }

    [Fact]
    public void Evaluate_CommercialWithoutModelRelease_Denied()
    {
        var rights = new AssetRights(TestAssetId, TestOwnerId, ReleaseStatus.None);
        rights.AddRestriction("Commercial requires model release",
            restrictedPurpose: UsagePurpose.Commercial,
            requiresRelease: ReleaseStatus.ModelRelease);

        var terms = MakeTerms(UsageChannel.Web, ["NO"], UsagePurpose.Commercial);

        var decision = rights.Evaluate(terms);

        Assert.False(decision.IsAllowed);
        Assert.Contains(decision.DenialReasons, r => r.Contains("requires ModelRelease"));
    }

    [Fact]
    public void Evaluate_CommercialWithModelRelease_Allowed()
    {
        var rights = new AssetRights(TestAssetId, TestOwnerId, ReleaseStatus.ModelRelease);
        rights.AddRestriction("Commercial requires model release",
            restrictedPurpose: UsagePurpose.Commercial,
            requiresRelease: ReleaseStatus.ModelRelease);

        var terms = MakeTerms(UsageChannel.Web, ["NO"], UsagePurpose.Commercial);

        var decision = rights.Evaluate(terms);

        Assert.True(decision.IsAllowed);
    }

    [Fact]
    public void Evaluate_MultipleRestrictions_ReturnsMergedDenialReasons()
    {
        var rights = new AssetRights(TestAssetId, TestOwnerId);
        rights.AddRestriction("No commercial", restrictedPurpose: UsagePurpose.Commercial);
        rights.AddRestriction("No web", restrictedChannel: UsageChannel.Web);

        var terms = MakeTerms(UsageChannel.Web, ["NO"], UsagePurpose.Commercial);

        var decision = rights.Evaluate(terms);

        Assert.False(decision.IsAllowed);
        Assert.Equal(2, decision.DenialReasons.Count);
    }

    // ──────────────────────────────────────────────
    //  Exclusive Windows
    // ──────────────────────────────────────────────

    [Fact]
    public void Evaluate_ExclusiveRequest_NoExistingWindows_ReturnsAllowed()
    {
        var rights = new AssetRights(TestAssetId, TestOwnerId);
        var terms = MakeTerms(UsageChannel.Web, ["NO"], UsagePurpose.Editorial, isExclusive: true);

        var decision = rights.Evaluate(terms);

        Assert.True(decision.IsAllowed);
    }

    [Fact]
    public void Evaluate_ExclusiveRequest_OverlappingWindow_ReturnsDenied()
    {
        var rights = new AssetRights(TestAssetId, TestOwnerId);
        var scope = MakeScope(UsageChannel.Web, ["NO"], UsagePurpose.Editorial);
        rights.ReserveExclusiveScope(LicenseGrantId.New(), scope);

        var terms = MakeTerms(UsageChannel.Web, ["NO"], UsagePurpose.Editorial, isExclusive: true);

        var decision = rights.Evaluate(terms);

        Assert.False(decision.IsAllowed);
        Assert.Contains(decision.DenialReasons, r => r.Contains("ExclusiveConflict"));
    }

    [Fact]
    public void ReserveExclusiveScope_OverlappingScope_ThrowsRightsViolationException()
    {
        var rights = new AssetRights(TestAssetId, TestOwnerId);
        var scope = MakeScope(UsageChannel.Web, ["NO"], UsagePurpose.Editorial);
        rights.ReserveExclusiveScope(LicenseGrantId.New(), scope);

        Assert.Throws<RightsViolationException>(() =>
            rights.ReserveExclusiveScope(LicenseGrantId.New(), scope));
    }

    [Fact]
    public void ReserveExclusiveScope_NonOverlappingScope_Succeeds()
    {
        var rights = new AssetRights(TestAssetId, TestOwnerId);
        var scope1 = MakeScope(UsageChannel.Web, ["NO"], UsagePurpose.Editorial);
        var scope2 = MakeScope(UsageChannel.Print, ["NO"], UsagePurpose.Editorial);

        rights.ReserveExclusiveScope(LicenseGrantId.New(), scope1);
        rights.ReserveExclusiveScope(LicenseGrantId.New(), scope2);

        Assert.Equal(2, rights.ExclusiveWindows.Count);
    }

    [Fact]
    public void RevokeExclusiveScope_RemovesWindow()
    {
        var rights = new AssetRights(TestAssetId, TestOwnerId);
        var grantId = LicenseGrantId.New();
        var scope = MakeScope(UsageChannel.Web, ["NO"], UsagePurpose.Editorial);
        rights.ReserveExclusiveScope(grantId, scope);

        rights.RevokeExclusiveScope(grantId);

        Assert.Empty(rights.ExclusiveWindows);
    }

    // ──────────────────────────────────────────────
    //  Restriction Management
    // ──────────────────────────────────────────────

    [Fact]
    public void AddRestriction_AddsToList()
    {
        var rights = new AssetRights(TestAssetId, TestOwnerId);

        var restriction = rights.AddRestriction("No print", restrictedChannel: UsageChannel.Print);

        Assert.Single(rights.Restrictions);
        Assert.Equal("No print", restriction.Description);
    }

    [Fact]
    public void RemoveRestriction_RemovesFromList()
    {
        var rights = new AssetRights(TestAssetId, TestOwnerId);
        var restriction = rights.AddRestriction("No print", restrictedChannel: UsageChannel.Print);

        rights.RemoveRestriction(restriction.Id);

        Assert.Empty(rights.Restrictions);
    }

    [Fact]
    public void RemoveRestriction_NonExistent_ThrowsInvalidOperationException()
    {
        var rights = new AssetRights(TestAssetId, TestOwnerId);

        Assert.Throws<InvalidOperationException>(() => rights.RemoveRestriction(Guid.NewGuid()));
    }

    // ──────────────────────────────────────────────
    //  Helpers
    // ──────────────────────────────────────────────

    private static LicenseTerms MakeTerms(
        UsageChannel channel,
        string[] codes,
        UsagePurpose purpose,
        bool isExclusive = false)
    {
        return new LicenseTerms(MakeScope(channel, codes, purpose), isExclusive);
    }

    private static LicenseScope MakeScope(UsageChannel channel, string[] codes, UsagePurpose purpose)
    {
        var territory = new Territory(codes);
        var timeWindow = new TimeWindow(
            new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));
        return new LicenseScope(channel, territory, timeWindow, purpose);
    }
}
