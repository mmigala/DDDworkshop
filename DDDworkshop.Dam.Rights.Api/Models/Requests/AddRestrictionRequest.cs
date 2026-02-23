namespace DDDworkshop.Dam.Rights.Api.Models.Requests;

/// <summary>
/// API request model for adding a restriction to an asset's rights profile.
/// </summary>
public sealed class AddRestrictionRequest
{
    public string Description { get; init; } = default!;
    public string? RestrictedChannel { get; init; }
    public string? RestrictedPurpose { get; init; }
    public List<string>? RestrictedTerritoryCodes { get; init; }
    public string? RequiresRelease { get; init; }
}
