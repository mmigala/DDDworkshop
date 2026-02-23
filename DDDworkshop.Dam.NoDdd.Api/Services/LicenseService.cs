namespace DDDworkshop.Dam.NoDdd.Api.Services;

using DDDworkshop.Dam.NoDdd.Api.Data;
using DDDworkshop.Dam.NoDdd.Api.Entities;

// ⚠️ ANTI-PATTERN: Another "Service Blob" — all grant management logic in one class.
//
// Problems:
//   1. Entity lifecycle (Issue → Revoke → Expire) is managed externally, not by the entity itself.
//   2. The entity has public setters, so ANY code can change Status, RevokedAt, etc. at any time.
//   3. No domain events — side effects (notifications, indexing) would be manually triggered
//      from every call site, easy to forget.
//   4. Exclusivity conflict check is DUPLICATED from RightsService.
//   5. State transition guards (e.g., "cannot revoke expired") are here, not in the entity.
//      Another developer could bypass this service and set Status = "Revoked" directly.

/// <summary>
/// Service containing all license grant management logic.
/// </summary>
public class LicenseService
{
    private readonly InMemoryDataStore _store;

    public LicenseService(InMemoryDataStore store)
    {
        _store = store;
    }

    // ──────────────────────────────────────────────
    //  Issue / Revoke
    // ──────────────────────────────────────────────

    /// <summary>
    /// Issues a new license grant if the rights evaluation passes.
    /// </summary>
    public (bool IsAllowed, Guid? GrantId, List<string> DenialReasons) IssueLicense(
        Guid assetId,
        Guid licenseeId,
        string channel,
        string territory,
        DateTimeOffset start,
        DateTimeOffset end,
        string purpose,
        bool isExclusive)
    {
        if (!_store.Assets.ContainsKey(assetId))
            throw new InvalidOperationException($"Asset '{assetId}' not found.");

        // ⚠️ This service calls RightsService.EvaluateRights logic inline instead of
        // delegating properly. The "evaluate" code is either duplicated or tightly coupled.
        // Here we inline the evaluation to show the anti-pattern.

        var reasons = new List<string>();

        // --- Restriction checks (duplicated logic from RightsService) ---
        var asset = _store.Assets[assetId];
        var restrictions = _store.Restrictions.Values.Where(r => r.AssetId == assetId);

        foreach (var r in restrictions)
        {
            // ⚠️ Business rule duplicated from RightsService.RestrictionBlocks
            if (RestrictionBlocks(r, channel, territory, purpose, asset.ReleaseStatus))
            {
                if (r.RequiresRelease is not null)
                    reasons.Add($"{r.Description} (requires {r.RequiresRelease})");
                else
                    reasons.Add(r.Description);
            }
        }

        // --- Exclusive conflict check (duplicated from RightsService) ---
        if (isExclusive)
        {
            // ⚠️ Business rule duplicated from RightsService.ScopesOverlap
            var existingWindows = _store.ExclusiveWindows.Values
                .Where(w => w.AssetId == assetId);

            foreach (var w in existingWindows)
            {
                if (CheckExclusivityConflict(w, channel, territory, start, end, purpose))
                {
                    reasons.Add($"ExclusiveConflict: scope overlaps with existing exclusive grant '{w.GrantId}'");
                }
            }

            // Also check active exclusive grants
            var activeExclusiveGrants = _store.LicenseGrants.Values
                .Where(g => g.AssetId == assetId
                         && g.IsExclusive
                         && g.Status == "Issued"
                         && g.ExpiresAt > DateTimeOffset.UtcNow);

            foreach (var g in activeExclusiveGrants)
            {
                if (CheckGrantOverlap(g, channel, territory, start, end, purpose))
                {
                    reasons.Add($"ExclusiveConflict: scope overlaps with active exclusive grant '{g.Id}'");
                }
            }
        }

        if (reasons.Count > 0)
            return (false, null, reasons);

        // --- Create the grant entity ---
        var now = DateTimeOffset.UtcNow;
        var grant = new LicenseGrantEntity
        {
            // ⚠️ No factory method — entity is constructed with raw new + property assignment.
            // Nothing prevents creating an entity in an invalid state (e.g., Status = "Banana").
            Id = Guid.NewGuid(),
            AssetId = assetId,
            LicenseeId = licenseeId,
            Channel = channel,
            Territory = territory,
            Start = start,
            End = end,
            Purpose = purpose,
            IsExclusive = isExclusive,
            Status = "Issued",
            IssuedAt = now,
            ExpiresAt = end
        };

        _store.LicenseGrants[grant.Id] = grant;

        // ⚠️ No domain events raised — if we need to notify downstream systems,
        // we'd have to remember to add that call here AND in every other place that issues licenses.

        // If exclusive, create the exclusive window
        if (isExclusive)
        {
            var window = new ExclusiveWindowEntity
            {
                Id = Guid.NewGuid(),
                AssetId = assetId,
                GrantId = grant.Id,
                Channel = channel,
                Territory = territory,
                Start = start,
                End = end,
                Purpose = purpose
            };
            _store.ExclusiveWindows[window.Id] = window;
        }

        return (true, grant.Id, []);
    }

    /// <summary>
    /// Revokes a license grant.
    /// </summary>
    public void RevokeLicense(Guid grantId, string reason, string revokedBy)
    {
        if (!_store.LicenseGrants.TryGetValue(grantId, out var grant))
            throw new InvalidOperationException($"License grant '{grantId}' not found.");

        // ⚠️ State guard is in the service, NOT in the entity.
        // Any other code that has access to the entity can bypass this check:
        //   grant.Status = "Revoked";  // No one stops you!
        if (grant.Status == "Expired")
            throw new InvalidOperationException("Cannot revoke an expired grant.");
        if (grant.Status == "Revoked")
            throw new InvalidOperationException("Grant is already revoked.");

        // ⚠️ Direct mutation — no encapsulation, no audit trail built-in.
        grant.Status = "Revoked";
        grant.RevocationReason = reason;
        grant.RevokedBy = revokedBy;
        grant.RevokedAt = DateTimeOffset.UtcNow;

        // ⚠️ No domain events — downstream systems won't be notified unless we
        // manually add notification code here.

        // Remove exclusive windows for this grant
        var windowsToRemove = _store.ExclusiveWindows.Values
            .Where(w => w.GrantId == grantId)
            .Select(w => w.Id)
            .ToList();

        foreach (var windowId in windowsToRemove)
        {
            _store.ExclusiveWindows.TryRemove(windowId, out _);
        }
    }

    // ──────────────────────────────────────────────
    //  Queries
    // ──────────────────────────────────────────────

    public LicenseGrantEntity? GetGrant(Guid grantId)
    {
        _store.LicenseGrants.TryGetValue(grantId, out var grant);
        return grant;
    }

    public List<LicenseGrantEntity> GetGrantsForAsset(Guid assetId, bool activeOnly)
    {
        var now = DateTimeOffset.UtcNow;
        var grants = _store.LicenseGrants.Values
            .Where(g => g.AssetId == assetId);

        if (activeOnly)
        {
            grants = grants.Where(g => g.Status == "Issued" && g.ExpiresAt > now);
        }

        return grants.ToList();
    }

    // ──────────────────────────────────────────────
    //  Private Helpers — duplicated business logic
    // ──────────────────────────────────────────────

    // ⚠️ DUPLICATED from RightsService.RestrictionBlocks — same logic, second copy.
    // If someone fixes a bug in RightsService, they might forget to fix it here.
    private static bool RestrictionBlocks(
        RestrictionEntity restriction,
        string requestedChannel,
        string requestedTerritory,
        string requestedPurpose,
        string assetReleaseStatus)
    {
        if (restriction.Channel is not null &&
            !string.Equals(restriction.Channel, requestedChannel, StringComparison.OrdinalIgnoreCase))
            return false;

        if (restriction.Purpose is not null &&
            !string.Equals(restriction.Purpose, requestedPurpose, StringComparison.OrdinalIgnoreCase))
            return false;

        if (restriction.Territory is not null)
        {
            var restrictedCodes = restriction.Territory.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var requestedCodes = requestedTerritory.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (!restrictedCodes.Intersect(requestedCodes, StringComparer.OrdinalIgnoreCase).Any())
                return false;
        }

        if (restriction.RequiresRelease is not null)
        {
            if (assetReleaseStatus.Contains(restriction.RequiresRelease, StringComparison.OrdinalIgnoreCase)
                || string.Equals(assetReleaseStatus, "Both", StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        return true;
    }

    // ⚠️ DUPLICATED from RightsService.ScopesOverlap — same overlap algorithm, second copy.
    private static bool CheckExclusivityConflict(
        ExclusiveWindowEntity window,
        string channel,
        string territory,
        DateTimeOffset start,
        DateTimeOffset end,
        string purpose)
    {
        return RightsService.ScopesOverlap(
            window.Channel, window.Territory, window.Start, window.End, window.Purpose,
            channel, territory, start, end, purpose);
    }

    // ⚠️ Yet another overlap check variant — for grants directly.
    private static bool CheckGrantOverlap(
        LicenseGrantEntity grant,
        string channel,
        string territory,
        DateTimeOffset start,
        DateTimeOffset end,
        string purpose)
    {
        return RightsService.ScopesOverlap(
            grant.Channel, grant.Territory, grant.Start, grant.End, grant.Purpose,
            channel, territory, start, end, purpose);
    }
}
