namespace DDDworkshop.Dam.NoDdd.Api.Entities;

// ⚠️ ANTI-PATTERN: Anemic entity — no behavior, no invariant protection.
// The service layer is responsible for all validation, scattered across methods.

/// <summary>
/// Mutable entity representing a restriction on an asset.
/// </summary>
public class RestrictionEntity
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public string Description { get; set; } = string.Empty;

    // ⚠️ Nullable strings instead of typed enums — no compile-time safety.
    // "Wbe" instead of "Web" won't be caught until runtime (maybe).
    public string? Channel { get; set; }
    public string? Purpose { get; set; }

    // ⚠️ Territory as raw string — no validation, no overlap detection at the type level.
    // Compare to the DDD Territory value object that enforces ISO codes + overlap logic.
    public string? Territory { get; set; }

    // ⚠️ Release requirement as raw string (e.g., "ModelRelease").
    public string? RequiresRelease { get; set; }
}
