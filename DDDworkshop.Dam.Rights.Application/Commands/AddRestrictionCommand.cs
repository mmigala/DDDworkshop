namespace DDDworkshop.Dam.Rights.Application.Commands;

/// <summary>
/// Command to add a restriction to an asset's rights profile.
/// </summary>
public sealed record AddRestrictionCommand(
    Guid AssetId,
    string Description,
    string? RestrictedChannel,
    string? RestrictedPurpose,
    IReadOnlyList<string>? RestrictedTerritoryCodes,
    string? RequiresRelease);
