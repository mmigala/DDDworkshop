namespace DDDworkshop.Dam.Rights.Application.Dtos;

/// <summary>
/// DTO representing an asset's rights profile.
/// </summary>
public sealed record AssetRightsProfileDto(
    Guid AssetId,
    Guid OwnerId,
    string ReleaseStatus,
    IReadOnlyList<RestrictionDto> Restrictions,
    IReadOnlyList<ExclusiveWindowDto> ExclusiveWindows);

public sealed record RestrictionDto(
    Guid Id,
    string Description,
    string? RestrictedChannel,
    string? RestrictedPurpose,
    IReadOnlyList<string>? RestrictedTerritoryCodes,
    string? RequiresRelease);

public sealed record ExclusiveWindowDto(
    Guid Id,
    Guid GrantId,
    string Channel,
    IReadOnlyList<string> TerritoryCodes,
    DateTimeOffset TimeWindowStart,
    DateTimeOffset TimeWindowEnd,
    string Purpose);
