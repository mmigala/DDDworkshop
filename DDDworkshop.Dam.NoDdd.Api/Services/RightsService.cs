namespace DDDworkshop.Dam.NoDdd.Api.Services;

using DDDworkshop.Dam.NoDdd.Api.Data;
using DDDworkshop.Dam.NoDdd.Api.Entities;

// ⚠️ ANTI-PATTERN: "Service Blob" — a large class that contains ALL business rules
// related to asset rights. In a real codebase this would easily grow to 500–800+ lines.
//
// Problems:
//   1. Business rules live here, NOT in the entities. Entities are just data bags.
//   2. Territory overlap logic is implemented inline with raw string splitting — 
//      no reusable, testable value object.
//   3. Restriction matching logic cannot be unit-tested without the full service + data store.
//   4. Some exclusivity logic is duplicated in LicenseService (see below).
//   5. Any new endpoint can bypass this service and mutate entities directly.

/// <summary>
/// Service containing all rights-related business logic.
/// </summary>
public class RightsService
{
    private readonly InMemoryDataStore _store;

    public RightsService(InMemoryDataStore store)
    {
        _store = store;
    }

    // ──────────────────────────────────────────────
    //  Rights Profile Management
    // ──────────────────────────────────────────────

    public AssetEntity SetRightsProfile(Guid assetId, Guid ownerId, string releaseStatus)
    {
        // ⚠️ No validation on releaseStatus — any string is accepted ("Banana" would work).
        // In the DDD version, ReleaseStatus is a [Flags] enum enforced at compile time.

        if (_store.Assets.TryGetValue(assetId, out var existing))
        {
            // ⚠️ Direct mutation of entity fields — no encapsulation.
            existing.OwnerId = ownerId;
            existing.ReleaseStatus = releaseStatus;
            return existing;
        }

        var entity = new AssetEntity
        {
            Id = assetId,
            OwnerId = ownerId,
            ReleaseStatus = releaseStatus
        };

        _store.Assets[assetId] = entity;
        return entity;
    }

    public RestrictionEntity AddRestriction(
        Guid assetId,
        string description,
        string? channel,
        string? purpose,
        string? territory,
        string? requiresRelease)
    {
        if (!_store.Assets.ContainsKey(assetId))
            throw new InvalidOperationException($"Asset '{assetId}' not found.");

        // ⚠️ No validation that channel/purpose are valid enum values.
        // "Wbe" instead of "Web" won't be caught here.

        var entity = new RestrictionEntity
        {
            Id = Guid.NewGuid(),
            AssetId = assetId,
            Description = description,
            Channel = channel,
            Purpose = purpose,
            Territory = territory,
            RequiresRelease = requiresRelease
        };

        _store.Restrictions[entity.Id] = entity;
        return entity;
    }

    public ExclusiveWindowEntity AddExclusiveWindow(
        Guid assetId,
        Guid grantId,
        string channel,
        string territory,
        DateTimeOffset start,
        DateTimeOffset end,
        string purpose)
    {
        if (!_store.Assets.ContainsKey(assetId))
            throw new InvalidOperationException($"Asset '{assetId}' not found.");

        // ⚠️ Duplicated overlap check — same logic exists in LicenseService.IssueLicense.
        // If someone changes the overlap rule here but forgets LicenseService, the system is inconsistent.
        var existingWindows = _store.ExclusiveWindows.Values
            .Where(w => w.AssetId == assetId);

        foreach (var w in existingWindows)
        {
            if (ScopesOverlap(w.Channel, w.Territory, w.Start, w.End, w.Purpose,
                              channel, territory, start, end, purpose))
            {
                throw new InvalidOperationException(
                    $"Exclusive scope conflicts with existing window for grant '{w.GrantId}'.");
            }
        }

        var entity = new ExclusiveWindowEntity
        {
            Id = Guid.NewGuid(),
            AssetId = assetId,
            GrantId = grantId,
            Channel = channel,
            Territory = territory,
            Start = start,
            End = end,
            Purpose = purpose
        };

        _store.ExclusiveWindows[entity.Id] = entity;
        return entity;
    }

    // ──────────────────────────────────────────────
    //  Rights Evaluation
    // ──────────────────────────────────────────────

    /// <summary>
    /// Evaluates whether the requested license is allowed.
    /// Returns (isAllowed, denialReasons).
    /// </summary>
    public (bool IsAllowed, List<string> DenialReasons) EvaluateRights(
        Guid assetId,
        string channel,
        string territory,
        DateTimeOffset start,
        DateTimeOffset end,
        string purpose,
        bool isExclusive)
    {
        if (!_store.Assets.TryGetValue(assetId, out var asset))
            throw new InvalidOperationException($"Asset '{assetId}' not found.");

        var reasons = new List<string>();

        // ⚠️ All restriction-matching logic is inline in this method.
        // Cannot be tested independently. In the DDD version, each RightRestriction
        // has a Blocks() method that can be unit-tested in isolation.

        var restrictions = _store.Restrictions.Values
            .Where(r => r.AssetId == assetId);

        foreach (var r in restrictions)
        {
            if (RestrictionBlocks(r, channel, territory, purpose, asset.ReleaseStatus))
            {
                if (r.RequiresRelease is not null)
                    reasons.Add($"{r.Description} (requires {r.RequiresRelease})");
                else
                    reasons.Add(r.Description);
            }
        }

        // Check exclusive window conflicts
        if (isExclusive)
        {
            var existingWindows = _store.ExclusiveWindows.Values
                .Where(w => w.AssetId == assetId);

            foreach (var w in existingWindows)
            {
                if (ScopesOverlap(w.Channel, w.Territory, w.Start, w.End, w.Purpose,
                                  channel, territory, start, end, purpose))
                {
                    reasons.Add($"ExclusiveConflict: scope overlaps with existing exclusive grant '{w.GrantId}'");
                }
            }
        }

        return (reasons.Count == 0, reasons);
    }

    // ──────────────────────────────────────────────
    //  Query Helpers
    // ──────────────────────────────────────────────

    public AssetEntity? GetAsset(Guid assetId)
    {
        _store.Assets.TryGetValue(assetId, out var asset);
        return asset;
    }

    public List<RestrictionEntity> GetRestrictions(Guid assetId)
    {
        return _store.Restrictions.Values.Where(r => r.AssetId == assetId).ToList();
    }

    public List<ExclusiveWindowEntity> GetExclusiveWindows(Guid assetId)
    {
        return _store.ExclusiveWindows.Values.Where(w => w.AssetId == assetId).ToList();
    }

    // ──────────────────────────────────────────────
    //  Private Helpers — inlined business logic
    // ──────────────────────────────────────────────

    // ⚠️ This method does the same job as RightRestriction.Blocks() in the DDD version,
    // but it's a private helper in a service — not testable in isolation, not discoverable,
    // and duplicated logic if another service needs the same check.
    private static bool RestrictionBlocks(
        RestrictionEntity restriction,
        string requestedChannel,
        string requestedTerritory,
        string requestedPurpose,
        string assetReleaseStatus)
    {
        // Channel match
        if (restriction.Channel is not null &&
            !string.Equals(restriction.Channel, requestedChannel, StringComparison.OrdinalIgnoreCase))
            return false;

        // Purpose match
        if (restriction.Purpose is not null &&
            !string.Equals(restriction.Purpose, requestedPurpose, StringComparison.OrdinalIgnoreCase))
            return false;

        // Territory overlap (raw string splitting)
        if (restriction.Territory is not null)
        {
            // ⚠️ Territory as comma-separated strings — fragile, no ISO code validation.
            var restrictedCodes = restriction.Territory.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var requestedCodes = requestedTerritory.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (!restrictedCodes.Intersect(requestedCodes, StringComparer.OrdinalIgnoreCase).Any())
                return false;
        }

        // Release requirement
        if (restriction.RequiresRelease is not null)
        {
            // ⚠️ String comparison for flags — fragile. "ModelRelease" vs "modelrelease" vs "Both"
            // In the DDD version this is a proper [Flags] enum with bitwise operations.
            if (assetReleaseStatus.Contains(restriction.RequiresRelease, StringComparison.OrdinalIgnoreCase)
                || string.Equals(assetReleaseStatus, "Both", StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        return true;
    }

    // ⚠️ DUPLICATED: This same overlap logic also exists in LicenseService.CheckExclusivityConflict.
    // If the overlap algorithm changes, both places must be updated.
    // In the DDD version, LicenseScope.OverlapsWith() is a single, testable method on a value object.
    internal static bool ScopesOverlap(
        string channel1, string territory1, DateTimeOffset start1, DateTimeOffset end1, string purpose1,
        string channel2, string territory2, DateTimeOffset start2, DateTimeOffset end2, string purpose2)
    {
        // Channel must match
        if (!string.Equals(channel1, channel2, StringComparison.OrdinalIgnoreCase))
            return false;

        // Purpose must match
        if (!string.Equals(purpose1, purpose2, StringComparison.OrdinalIgnoreCase))
            return false;

        // Time windows must overlap
        if (start1 >= end2 || start2 >= end1)
            return false;

        // Territory must overlap
        var codes1 = territory1.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var codes2 = territory2.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (!codes1.Intersect(codes2, StringComparer.OrdinalIgnoreCase).Any())
            return false;

        return true;
    }
}
