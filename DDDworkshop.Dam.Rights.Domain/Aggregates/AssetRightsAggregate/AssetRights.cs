namespace DDDworkshop.Dam.Rights.Domain.Aggregates.AssetRightsAggregate;

using DDDworkshop.Dam.Rights.Domain.Exceptions;
using DDDworkshop.Dam.Rights.Domain.SeedWork;
using DDDworkshop.Dam.Rights.Domain.ValueObjects;

/// <summary>
/// Aggregate Root: AssetRights
/// 
/// Represents the rights profile attached to an asset – the legal constraints
/// that determine whether a license request is allowed or denied.
/// 
/// This aggregate enforces all invariants related to:
///   - Restrictions (what usage is forbidden)
///   - Exclusive windows (what scope is reserved for a single licensee)
///   - Release status (model/property releases)
/// 
/// No external code can bypass these rules because all state changes
/// go through methods on the aggregate root (no public setters).
/// </summary>
public sealed class AssetRights : AggregateRoot<AssetId>
{
    private readonly List<RightRestriction> _restrictions = [];
    private readonly List<ExclusiveWindow> _exclusiveWindows = [];

    /// <summary>The owner / licensor of the asset.</summary>
    public OwnerId OwnerId { get; private set; }

    /// <summary>The current release status of the asset (model release, property release, etc.).</summary>
    public ReleaseStatus ReleaseStatus { get; private set; }

    /// <summary>Restrictions that limit how the asset can be used.</summary>
    public IReadOnlyList<RightRestriction> Restrictions => _restrictions.AsReadOnly();

    /// <summary>Active exclusive windows reserving a scope for a single licensee.</summary>
    public IReadOnlyList<ExclusiveWindow> ExclusiveWindows => _exclusiveWindows.AsReadOnly();

    public AssetRights(AssetId id, OwnerId ownerId, ReleaseStatus releaseStatus = ReleaseStatus.None)
        : base(id)
    {
        OwnerId = ownerId ?? throw new ArgumentNullException(nameof(ownerId));
        ReleaseStatus = releaseStatus;
    }

    // ──────────────────────────────────────────────
    //  Rights Profile Management (admin actions)
    // ──────────────────────────────────────────────

    /// <summary>
    /// Updates the owner and release status of the asset.
    /// </summary>
    public void UpdateProfile(OwnerId ownerId, ReleaseStatus releaseStatus)
    {
        OwnerId = ownerId ?? throw new ArgumentNullException(nameof(ownerId));
        ReleaseStatus = releaseStatus;
    }

    /// <summary>
    /// Adds a restriction to the asset's rights profile.
    /// </summary>
    public RightRestriction AddRestriction(
        string description,
        UsageChannel? restrictedChannel = null,
        UsagePurpose? restrictedPurpose = null,
        Territory? restrictedTerritory = null,
        ReleaseStatus? requiresRelease = null)
    {
        var restriction = new RightRestriction(
            Guid.NewGuid(),
            description,
            restrictedChannel,
            restrictedPurpose,
            restrictedTerritory,
            requiresRelease);

        _restrictions.Add(restriction);
        return restriction;
    }

    /// <summary>
    /// Removes a restriction by its id.
    /// </summary>
    public void RemoveRestriction(Guid restrictionId)
    {
        var restriction = _restrictions.Find(r => r.Id == restrictionId)
            ?? throw new InvalidOperationException($"Restriction '{restrictionId}' not found.");

        _restrictions.Remove(restriction);
    }

    // ──────────────────────────────────────────────
    //  License Evaluation (core domain logic)
    // ──────────────────────────────────────────────

    /// <summary>
    /// Evaluates whether the requested license terms are allowed based on
    /// the asset's restrictions and exclusive windows.
    /// 
    /// This is the heart of the domain logic for this aggregate.
    /// </summary>
    public RightsDecision Evaluate(LicenseTerms requestedTerms)
    {
        ArgumentNullException.ThrowIfNull(requestedTerms);

        var decisions = new List<RightsDecision>();

        // 1. Check restrictions
        decisions.Add(EvaluateRestrictions(requestedTerms.Scope));

        // 2. Check exclusive window conflicts (only if requesting exclusive)
        if (requestedTerms.IsExclusive)
        {
            decisions.Add(EvaluateExclusivity(requestedTerms.Scope));
        }

        return RightsDecision.Combine(decisions);
    }

    /// <summary>
    /// Checks if any restriction blocks the requested scope.
    /// </summary>
    private RightsDecision EvaluateRestrictions(LicenseScope scope)
    {
        var denialReasons = _restrictions
            .Where(r => r.Blocks(scope, ReleaseStatus))
            .Select(r => r.GetDenialReason())
            .ToList();

        return denialReasons.Count > 0
            ? RightsDecision.Denied(denialReasons)
            : RightsDecision.Allowed();
    }

    /// <summary>
    /// Checks if an existing exclusive window conflicts with the requested scope.
    /// </summary>
    private RightsDecision EvaluateExclusivity(LicenseScope scope)
    {
        var conflicting = _exclusiveWindows.FirstOrDefault(w => w.ConflictsWith(scope));

        return conflicting is not null
            ? RightsDecision.Denied($"ExclusiveConflict: scope overlaps with existing exclusive grant '{conflicting.GrantId}'")
            : RightsDecision.Allowed();
    }

    // ──────────────────────────────────────────────
    //  Exclusive Window Management
    // ──────────────────────────────────────────────

    /// <summary>
    /// Reserves an exclusive scope for a granted license.
    /// Called after a license grant is issued with IsExclusive = true.
    /// 
    /// Invariant: no overlapping exclusive windows are allowed.
    /// </summary>
    public void ReserveExclusiveScope(LicenseGrantId grantId, LicenseScope scope)
    {
        ArgumentNullException.ThrowIfNull(grantId);
        ArgumentNullException.ThrowIfNull(scope);

        // Enforce invariant: no overlap with existing exclusive windows
        var conflicting = _exclusiveWindows.FirstOrDefault(w => w.ConflictsWith(scope));
        if (conflicting is not null)
        {
            throw new RightsViolationException(
                $"Cannot reserve exclusive scope: conflicts with existing exclusive grant '{conflicting.GrantId}'.");
        }

        var window = new ExclusiveWindow(Guid.NewGuid(), grantId, scope);
        _exclusiveWindows.Add(window);
    }

    /// <summary>
    /// Removes the exclusive window for a given grant (e.g., when the grant is revoked or expires).
    /// </summary>
    public void RevokeExclusiveScope(LicenseGrantId grantId)
    {
        ArgumentNullException.ThrowIfNull(grantId);

        var removed = _exclusiveWindows.RemoveAll(w => w.GrantId == grantId);

        if (removed == 0)
            throw new InvalidOperationException($"No exclusive window found for grant '{grantId}'.");
    }
}
