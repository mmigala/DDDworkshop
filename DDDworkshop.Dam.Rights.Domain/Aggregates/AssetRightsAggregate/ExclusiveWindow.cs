namespace DDDworkshop.Dam.Rights.Domain.Aggregates.AssetRightsAggregate;

using DDDworkshop.Dam.Rights.Domain.SeedWork;
using DDDworkshop.Dam.Rights.Domain.ValueObjects;

/// <summary>
/// An entity within the AssetRights aggregate.
/// Represents a period during which an asset is exclusively licensed for a specific scope.
/// 
/// When an exclusive license is granted, an ExclusiveWindow is reserved on the asset's rights profile.
/// No other exclusive grant with an overlapping scope can be issued while this window is active.
/// </summary>
public sealed class ExclusiveWindow : Entity<Guid>
{
    /// <summary>
    /// The grant that holds this exclusive reservation.
    /// </summary>
    public LicenseGrantId GrantId { get; }

    /// <summary>
    /// The scope that is exclusively reserved (channel + territory + time + purpose).
    /// </summary>
    public LicenseScope Scope { get; }

    public ExclusiveWindow(Guid id, LicenseGrantId grantId, LicenseScope scope)
        : base(id)
    {
        GrantId = grantId ?? throw new ArgumentNullException(nameof(grantId));
        Scope = scope ?? throw new ArgumentNullException(nameof(scope));
    }

    /// <summary>
    /// Returns true if this exclusive window conflicts with the given scope.
    /// A conflict exists when the scopes overlap (same channel, purpose, overlapping territory and time).
    /// </summary>
    public bool ConflictsWith(LicenseScope other)
    {
        return Scope.OverlapsWith(other);
    }
}
