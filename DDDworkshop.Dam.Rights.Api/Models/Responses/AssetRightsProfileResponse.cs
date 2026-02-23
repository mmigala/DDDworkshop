namespace DDDworkshop.Dam.Rights.Api.Models.Responses;

/// <summary>
/// API response representing an asset's rights profile.
/// </summary>
public sealed class AssetRightsProfileResponse
{
    public Guid AssetId { get; init; }
    public Guid OwnerId { get; init; }
    public string ReleaseStatus { get; init; } = default!;
    public IReadOnlyList<RestrictionResponse> Restrictions { get; init; } = [];
    public IReadOnlyList<ExclusiveWindowResponse> ExclusiveWindows { get; init; } = [];
}

public sealed class RestrictionResponse
{
    public Guid Id { get; init; }
    public string Description { get; init; } = default!;
    public string? RestrictedChannel { get; init; }
    public string? RestrictedPurpose { get; init; }
    public IReadOnlyList<string>? RestrictedTerritoryCodes { get; init; }
    public string? RequiresRelease { get; init; }
}

public sealed class ExclusiveWindowResponse
{
    public Guid Id { get; init; }
    public Guid GrantId { get; init; }
    public string Channel { get; init; } = default!;
    public IReadOnlyList<string> TerritoryCodes { get; init; } = [];
    public DateTimeOffset TimeWindowStart { get; init; }
    public DateTimeOffset TimeWindowEnd { get; init; }
    public string Purpose { get; init; } = default!;
}
