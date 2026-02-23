namespace DDDworkshop.Dam.NoDdd.Api.Entities;

// ⚠️ ANTI-PATTERN: Anemic entity with public setters everywhere.
// Any code in the codebase can change any field at any time.
// There is NO consistency boundary — the entity cannot protect its own invariants.

/// <summary>
/// Mutable entity representing an asset's rights profile.
/// This is just a data bag — no behavior, no validation.
/// </summary>
public class AssetEntity
{
    // ⚠️ No encapsulation: any code can change these fields directly
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }

    // ⚠️ ReleaseStatus is a raw string — no compile-time safety, no validation at the type level.
    // In the DDD version this is a [Flags] enum value object that prevents invalid states.
    public string ReleaseStatus { get; set; } = "None";
}
