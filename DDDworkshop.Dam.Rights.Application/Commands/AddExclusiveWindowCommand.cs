namespace DDDworkshop.Dam.Rights.Application.Commands;

/// <summary>
/// Command to add an exclusive window to an asset's rights profile.
/// </summary>
public sealed record AddExclusiveWindowCommand(
    Guid AssetId,
    Guid GrantId,
    string Channel,
    IReadOnlyList<string> TerritoryCodes,
    DateTimeOffset TimeWindowStart,
    DateTimeOffset TimeWindowEnd,
    string Purpose);
