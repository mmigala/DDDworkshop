namespace DDDworkshop.Dam.Rights.Application.Dtos;

/// <summary>
/// DTO representing a license grant.
/// </summary>
public sealed record LicenseGrantDto(
    Guid GrantId,
    Guid AssetId,
    Guid LicenseeId,
    string Channel,
    IReadOnlyList<string> TerritoryCodes,
    DateTimeOffset TimeWindowStart,
    DateTimeOffset TimeWindowEnd,
    string Purpose,
    bool IsExclusive,
    string Status,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt,
    string? RevocationReason,
    string? RevokedBy,
    DateTimeOffset? RevokedAt);
