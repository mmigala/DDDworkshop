namespace DDDworkshop.Dam.Rights.Api.Models.Requests;

/// <summary>
/// API request model for creating or updating an asset's rights profile.
/// </summary>
public sealed class SetRightsProfileRequest
{
    public Guid OwnerId { get; init; }
    public string ReleaseStatus { get; init; } = default!;
}
