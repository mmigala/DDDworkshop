namespace DDDworkshop.Dam.Rights.Tests.NoDddContrast;

using DDDworkshop.Dam.NoDdd.Api.Data;
using DDDworkshop.Dam.NoDdd.Api.Services;

/// <summary>
/// Contrast tests for the Non-DDD RightsService.
/// 
/// ⚠️ NOTICE how much setup is needed just to test a single business rule:
///   1. Create InMemoryDataStore
///   2. Create RightsService (depends on the store)
///   3. Pre-populate an AssetEntity via SetRightsProfile
///   4. Add restrictions via AddRestriction (raw strings, no validation)
///   5. Call EvaluateRights with 7 primitive parameters
/// 
/// Compare this to the DDD AssetRightsTests where you just instantiate an aggregate,
/// call AddRestriction (with strongly-typed enums), and call Evaluate().
/// 
/// Also notice: territory codes are raw comma-separated strings ("NO,SE"),
/// channels and purposes are unvalidated strings. "Wbe" won't fail until runtime.
/// </summary>
public class RightsServiceContrastTests
{
    // ──────────────────────────────────────────────
    //  ⚠️ Heavy setup required for every test
    // ──────────────────────────────────────────────

    private static (InMemoryDataStore Store, RightsService Service) CreateServiceWithAsset()
    {
        var store = new InMemoryDataStore();
        var service = new RightsService(store);
        var assetId = Guid.NewGuid();

        // Must pre-populate entity via service (no aggregate to construct)
        service.SetRightsProfile(assetId, Guid.NewGuid(), "None");

        return (store, service);
    }

    // ──────────────────────────────────────────────
    //  ⚠️ Same tests as DDD, but much more ceremony
    // ──────────────────────────────────────────────

    [Fact]
    public void EvaluateRights_NoRestrictions_ReturnsAllowed()
    {
        // Arrange — heavy: data store + service + entity pre-population
        var store = new InMemoryDataStore();
        var service = new RightsService(store);
        var assetId = Guid.NewGuid();
        service.SetRightsProfile(assetId, Guid.NewGuid(), "None");

        // Act — 7 primitive params, all strings, no compile-time safety
        var (isAllowed, reasons) = service.EvaluateRights(
            assetId, "Web", "NO", 
            new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            "Editorial", false);

        Assert.True(isAllowed);
        Assert.Empty(reasons);
    }

    [Fact]
    public void EvaluateRights_ChannelRestricted_ReturnsDenied()
    {
        var store = new InMemoryDataStore();
        var service = new RightsService(store);
        var assetId = Guid.NewGuid();
        service.SetRightsProfile(assetId, Guid.NewGuid(), "None");

        // ⚠️ Adding restriction with raw strings — "Prnt" typo won't be caught
        service.AddRestriction(assetId, "No print", "Print", null, null, null);

        var (isAllowed, reasons) = service.EvaluateRights(
            assetId, "Print", "NO",
            new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            "Editorial", false);

        Assert.False(isAllowed);
        Assert.Contains(reasons, r => r.Contains("No print"));
    }

    [Fact]
    public void EvaluateRights_TerritoryRestricted_Overlapping_ReturnsDenied()
    {
        var store = new InMemoryDataStore();
        var service = new RightsService(store);
        var assetId = Guid.NewGuid();
        service.SetRightsProfile(assetId, Guid.NewGuid(), "None");

        // ⚠️ Territory is a comma-separated string — no ISO validation
        service.AddRestriction(assetId, "No US distribution", "Web", null, "US", null);

        var (isAllowed, reasons) = service.EvaluateRights(
            assetId, "Web", "US,NO",
            new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            "Editorial", false);

        Assert.False(isAllowed);
    }

    [Fact]
    public void EvaluateRights_TypoInChannel_SilentlyPassesWhenItShouldFail()
    {
        // ⚠️ DDD CONTRAST: This demonstrates a bug that DDD prevents at compile time.
        // Passing "Wbe" instead of "Web" — no compile error, no runtime error,
        // the restriction simply doesn't match (silent bug).
        // In the DDD version, UsageChannel.Wbe wouldn't compile.

        var store = new InMemoryDataStore();
        var service = new RightsService(store);
        var assetId = Guid.NewGuid();
        service.SetRightsProfile(assetId, Guid.NewGuid(), "None");

        service.AddRestriction(assetId, "No web", "Wbe", null, null, null); // ⚠️ typo!

        var (isAllowed, _) = service.EvaluateRights(
            assetId, "Web", "NO",
            new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            "Editorial", false);

        // ⚠️ This is ALLOWED even though we meant to restrict Web usage!
        // The restriction was stored as "Wbe" so it doesn't match "Web".
        Assert.True(isAllowed); // Bug! Should be denied but passes silently.
    }

    [Fact]
    public void EvaluateRights_ReleaseRequirement_RawStringComparison()
    {
        // ⚠️ DDD CONTRAST: Release status is a raw string. "ModelRelease" vs "modelrelease"
        // depends on string comparison. In DDD it's a [Flags] enum with bitwise ops.

        var store = new InMemoryDataStore();
        var service = new RightsService(store);
        var assetId = Guid.NewGuid();
        service.SetRightsProfile(assetId, Guid.NewGuid(), "None"); // No releases

        service.AddRestriction(assetId, "Commercial requires model release",
            null, "Commercial", null, "ModelRelease");

        var (isAllowed, reasons) = service.EvaluateRights(
            assetId, "Web", "NO",
            new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            "Commercial", false);

        Assert.False(isAllowed);
        Assert.Contains(reasons, r => r.Contains("requires ModelRelease"));
    }
}
