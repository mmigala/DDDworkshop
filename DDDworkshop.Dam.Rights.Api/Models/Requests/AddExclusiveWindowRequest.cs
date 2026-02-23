namespace DDDworkshop.Dam.Rights.Api.Models.Requests;

/// <summary>
/// API request model for adding an exclusive window to an asset's rights profile.
/// </summary>
public sealed class AddExclusiveWindowRequest
{
    public Guid GrantId { get; init; }
    public string Channel { get; init; } = default!;
    public List<string> TerritoryCodes { get; init; } = [];
    public DateTimeOffset TimeWindowStart { get; init; }
    public DateTimeOffset TimeWindowEnd { get; init; }
    public string Purpose { get; init; } = default!;
}
