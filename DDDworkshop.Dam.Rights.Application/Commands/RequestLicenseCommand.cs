namespace DDDworkshop.Dam.Rights.Application.Commands;

/// <summary>
/// Command to request a license for an asset.
/// The handler will evaluate rights, check exclusivity, and issue a grant if allowed.
/// </summary>
public sealed record RequestLicenseCommand(
    Guid AssetId,
    Guid LicenseeId,
    string Channel,
    IReadOnlyList<string> TerritoryCodes,
    DateTimeOffset TimeWindowStart,
    DateTimeOffset TimeWindowEnd,
    string Purpose,
    bool IsExclusive);
