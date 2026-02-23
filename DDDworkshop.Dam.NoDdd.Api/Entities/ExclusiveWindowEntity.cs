namespace DDDworkshop.Dam.NoDdd.Api.Entities;

// ⚠️ ANTI-PATTERN: Anemic entity — no behavior, no invariant protection.
// Overlap detection lives in the service, not here. The entity can't protect itself.

/// <summary>
/// Mutable entity representing an exclusive window on an asset.
/// </summary>
public class ExclusiveWindowEntity
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public Guid GrantId { get; set; }

    // ⚠️ Raw strings — no type safety, no validation.
    public string Channel { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;

    // ⚠️ Territory as comma-separated string — no structured overlap detection.
    // Compare to the DDD Territory value object with OverlapsWith() built in.
    public string Territory { get; set; } = string.Empty;

    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }
}
