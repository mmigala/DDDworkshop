namespace DDDworkshop.Dam.Rights.Domain.Aggregates.AssetRightsAggregate;

using DDDworkshop.Dam.Rights.Domain.SeedWork;
using DDDworkshop.Dam.Rights.Domain.ValueObjects;

/// <summary>
/// An entity within the AssetRights aggregate.
/// Represents a single restriction on how an asset may be used.
/// 
/// Examples:
///   - "No commercial use" (RestrictedPurpose = Commercial)
///   - "No print channel" (RestrictedChannel = Print)
///   - "Not available in US" (RestrictedTerritory = { "US" })
///   - "Commercial requires ModelRelease" (RequiresRelease = ModelRelease, RestrictedPurpose = Commercial)
/// 
/// A restriction blocks a license request if the request matches all non-null fields.
/// Null fields mean "any" (wildcard).
/// </summary>
public sealed class RightRestriction : Entity<Guid>
{
    /// <summary>
    /// If set, this restriction applies only to requests for this channel.
    /// If null, the restriction applies to all channels.
    /// </summary>
    public UsageChannel? RestrictedChannel { get; }

    /// <summary>
    /// If set, this restriction applies only to requests for this purpose.
    /// If null, the restriction applies to all purposes.
    /// </summary>
    public UsagePurpose? RestrictedPurpose { get; }

    /// <summary>
    /// If set, this restriction applies only to requests targeting this territory.
    /// If null, the restriction applies to all territories.
    /// </summary>
    public Territory? RestrictedTerritory { get; }

    /// <summary>
    /// If set, the restricted purpose is only blocked when the asset lacks this release status.
    /// Used for rules like "Commercial requires ModelRelease".
    /// </summary>
    public ReleaseStatus? RequiresRelease { get; }

    /// <summary>
    /// Human-readable description of the restriction.
    /// </summary>
    public string Description { get; }

    public RightRestriction(
        Guid id,
        string description,
        UsageChannel? restrictedChannel = null,
        UsagePurpose? restrictedPurpose = null,
        Territory? restrictedTerritory = null,
        ReleaseStatus? requiresRelease = null)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Restriction description cannot be empty.", nameof(description));

        // At least one constraint must be specified
        if (restrictedChannel is null && restrictedPurpose is null && restrictedTerritory is null)
            throw new ArgumentException("A restriction must constrain at least one of: channel, purpose, or territory.");

        Description = description;
        RestrictedChannel = restrictedChannel;
        RestrictedPurpose = restrictedPurpose;
        RestrictedTerritory = restrictedTerritory;
        RequiresRelease = requiresRelease;
    }

    /// <summary>
    /// Evaluates whether this restriction blocks the given license scope.
    /// </summary>
    /// <param name="scope">The requested license scope.</param>
    /// <param name="assetReleaseStatus">The current release status of the asset.</param>
    /// <returns>True if the request is blocked by this restriction.</returns>
    public bool Blocks(LicenseScope scope, ReleaseStatus assetReleaseStatus)
    {
        // Check channel match (null = matches any)
        if (RestrictedChannel is not null && RestrictedChannel != scope.Channel)
            return false;

        // Check purpose match (null = matches any)
        if (RestrictedPurpose is not null && RestrictedPurpose != scope.Purpose)
            return false;

        // Check territory overlap (null = matches any territory)
        if (RestrictedTerritory is not null && !RestrictedTerritory.OverlapsWith(scope.Territory))
            return false;

        // If this restriction has a release requirement, check if the asset satisfies it
        if (RequiresRelease is not null)
        {
            // The restriction is only active (blocks) if the asset does NOT have the required release
            bool hasRequiredRelease = (assetReleaseStatus & RequiresRelease.Value) == RequiresRelease.Value;
            return !hasRequiredRelease;
        }

        // All non-null fields matched → this restriction blocks the request
        return true;
    }

    /// <summary>
    /// Returns a human-readable denial reason for this restriction.
    /// </summary>
    public string GetDenialReason()
    {
        if (RequiresRelease is not null)
            return $"{Description} (requires {RequiresRelease})";

        return Description;
    }
}
