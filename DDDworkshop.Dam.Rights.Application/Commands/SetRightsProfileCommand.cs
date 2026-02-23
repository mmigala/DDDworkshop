namespace DDDworkshop.Dam.Rights.Application.Commands;

/// <summary>
/// Command to create or update an asset's rights profile.
/// </summary>
public sealed record SetRightsProfileCommand(
    Guid AssetId,
    Guid OwnerId,
    string ReleaseStatus);
