namespace DDDworkshop.Dam.Rights.Domain.ValueObjects;

/// <summary>
/// Tracks whether required model/property releases exist for an asset.
/// Commercial use typically requires at least a ModelRelease.
/// </summary>
[Flags]
public enum ReleaseStatus
{
    None = 0,
    ModelRelease = 1,
    PropertyRelease = 2,
    Both = ModelRelease | PropertyRelease
}
