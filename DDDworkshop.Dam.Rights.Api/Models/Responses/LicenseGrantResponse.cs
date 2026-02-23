namespace DDDworkshop.Dam.Rights.Api.Models.Responses;

/// <summary>
/// API response representing a license grant.
/// </summary>
public sealed class LicenseGrantResponse
{
    public Guid GrantId { get; init; }
    public Guid AssetId { get; init; }
    public Guid LicenseeId { get; init; }
    public string Channel { get; init; } = default!;
    public IReadOnlyList<string> TerritoryCodes { get; init; } = [];
    public DateTimeOffset TimeWindowStart { get; init; }
    public DateTimeOffset TimeWindowEnd { get; init; }
    public string Purpose { get; init; } = default!;
    public bool IsExclusive { get; init; }
    public string Status { get; init; } = default!;
    public DateTimeOffset IssuedAt { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
    public string? RevocationReason { get; init; }
    public string? RevokedBy { get; init; }
    public DateTimeOffset? RevokedAt { get; init; }
}
