namespace DDDworkshop.Dam.Rights.Tests.NoDddContrast;

using DDDworkshop.Dam.NoDdd.Api.Data;
using DDDworkshop.Dam.NoDdd.Api.Entities;
using DDDworkshop.Dam.NoDdd.Api.Services;

/// <summary>
/// Contrast tests for the Non-DDD LicenseService.
/// 
/// ⚠️ NOTICE these problems that DDD eliminates:
///   1. Setup requires InMemoryDataStore + RightsService + LicenseService.
///   2. Entity lifecycle (Issue/Revoke/Expire) is managed by the service, not the entity.
///      The entity has public setters — anyone can change Status directly.
///   3. Restriction-matching logic is DUPLICATED between RightsService and LicenseService.
///   4. No domain events — can't assert that side effects were triggered.
///   5. All parameters are primitive strings — typos are silent bugs.
/// 
/// Compare to LicenseGrantTests which create a single aggregate and test transitions
/// with zero infrastructure.
/// </summary>
public class LicenseServiceContrastTests
{
    [Fact]
    public void IssueLicense_NoRestrictions_ReturnsAllowed()
    {
        // ⚠️ Heavy setup: 3 objects needed just to test license issuance
        var store = new InMemoryDataStore();
        var rightsService = new RightsService(store); // needed to create the asset
        var licenseService = new LicenseService(store);

        var assetId = Guid.NewGuid();
        rightsService.SetRightsProfile(assetId, Guid.NewGuid(), "None");

        var (isAllowed, grantId, reasons) = licenseService.IssueLicense(
            assetId, Guid.NewGuid(), "Web", "NO",
            new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            "Editorial", false);

        Assert.True(isAllowed);
        Assert.NotNull(grantId);
        Assert.Empty(reasons);
    }

    [Fact]
    public void IssueLicense_WithRestriction_ReturnsDenied()
    {
        var store = new InMemoryDataStore();
        var rightsService = new RightsService(store);
        var licenseService = new LicenseService(store);

        var assetId = Guid.NewGuid();
        rightsService.SetRightsProfile(assetId, Guid.NewGuid(), "None");
        rightsService.AddRestriction(assetId, "No web", "Web", null, null, null);

        var (isAllowed, grantId, reasons) = licenseService.IssueLicense(
            assetId, Guid.NewGuid(), "Web", "NO",
            new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            "Editorial", false);

        Assert.False(isAllowed);
        Assert.Null(grantId);
    }

    [Fact]
    public void RevokeLicense_ValidGrant_SetsStatusToRevoked()
    {
        // ⚠️ Revoking requires setting up a full issuance first
        var store = new InMemoryDataStore();
        var rightsService = new RightsService(store);
        var licenseService = new LicenseService(store);

        var assetId = Guid.NewGuid();
        rightsService.SetRightsProfile(assetId, Guid.NewGuid(), "None");

        var (_, grantId, _) = licenseService.IssueLicense(
            assetId, Guid.NewGuid(), "Web", "NO",
            new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            "Editorial", false);

        licenseService.RevokeLicense(grantId!.Value, "Breach", "admin");

        // ⚠️ Must reach into the data store directly to verify — no aggregate to query
        var grant = store.LicenseGrants[grantId!.Value];
        Assert.Equal("Revoked", grant.Status);
    }

    [Fact]
    public void RevokeLicense_AlreadyRevoked_Throws()
    {
        var store = new InMemoryDataStore();
        var rightsService = new RightsService(store);
        var licenseService = new LicenseService(store);

        var assetId = Guid.NewGuid();
        rightsService.SetRightsProfile(assetId, Guid.NewGuid(), "None");

        var (_, grantId, _) = licenseService.IssueLicense(
            assetId, Guid.NewGuid(), "Web", "NO",
            new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            "Editorial", false);

        licenseService.RevokeLicense(grantId!.Value, "First", "admin");

        // ⚠️ Guard clause is in the SERVICE, not the entity.
        // bypass: store.LicenseGrants[grantId.Value].Status = "Issued"; // resets it!
        Assert.Throws<InvalidOperationException>(() =>
            licenseService.RevokeLicense(grantId!.Value, "Second", "admin"));
    }

    [Fact]
    public void EntityPublicSetters_AllowBypassingGuards()
    {
        // ⚠️ DDD CONTRAST: This test demonstrates the core encapsulation problem.
        // Entities have public setters, so ANY code can mutate state without going
        // through the service's guard clauses.

        var store = new InMemoryDataStore();
        var rightsService = new RightsService(store);
        var licenseService = new LicenseService(store);

        var assetId = Guid.NewGuid();
        rightsService.SetRightsProfile(assetId, Guid.NewGuid(), "None");

        var (_, grantId, _) = licenseService.IssueLicense(
            assetId, Guid.NewGuid(), "Web", "NO",
            new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            "Editorial", false);

        // ⚠️ Direct mutation bypasses all guards — this is the "anemic entity" problem
        var grant = store.LicenseGrants[grantId!.Value];
        grant.Status = "Banana"; // No compile error, no runtime error — invalid state!
        grant.Status = "Expired";
        grant.Status = "Revoked"; // Went Issued → Banana → Expired → Revoked with no audit trail

        Assert.Equal("Revoked", grant.Status); // "Succeeds" but state integrity is gone

        // In the DDD version, LicenseGrant has private setters.
        // The ONLY way to revoke is: grant.Revoke(reason, revokedBy, now)
        // which enforces invariants and records audit history.
    }

    [Fact]
    public void NoDomainEvents_SideEffectsAreInvisible()
    {
        // ⚠️ DDD CONTRAST: After issuing a license in the Non-DDD project, 
        // there's no way to check if events were raised — because there ARE no events.
        // In the DDD version, we can assert grant.DomainEvents contains LicenseGrantedEvent.

        var store = new InMemoryDataStore();
        var rightsService = new RightsService(store);
        var licenseService = new LicenseService(store);

        var assetId = Guid.NewGuid();
        rightsService.SetRightsProfile(assetId, Guid.NewGuid(), "None");

        var (_, grantId, _) = licenseService.IssueLicense(
            assetId, Guid.NewGuid(), "Web", "NO",
            new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            "Editorial", false);

        // ⚠️ There's no domain event to inspect.
        // Any downstream processes (notifications, indexing, auditing) would need
        // to be manually called from within IssueLicense — easy to forget in new code paths.
        var grant = store.LicenseGrants[grantId!.Value];
        Assert.NotNull(grant); // That's all we can check — no events, no audit trail
    }
}
