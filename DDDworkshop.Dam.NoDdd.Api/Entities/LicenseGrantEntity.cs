namespace DDDworkshop.Dam.NoDdd.Api.Entities;

// ⚠️ ANTI-PATTERN: Anemic entity with all public setters.
// State management (Issued → Revoked → Expired) is done externally by services.
// The entity itself has NO lifecycle methods, NO guards, NO domain events.
//
// Compare to the DDD LicenseGrant aggregate which:
//   - Enforces state transition rules (cannot revoke expired)
//   - Raises domain events on Issue/Revoke
//   - Tracks full audit history internally
//   - Has a factory method (Issue) as the only way to create instances

/// <summary>
/// Mutable entity representing a license grant.
/// </summary>
public class LicenseGrantEntity
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public Guid LicenseeId { get; set; }

    // ⚠️ Raw strings — typos won't be caught at compile time.
    public string Channel { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;

    // ⚠️ Territory as comma-separated string — no structured validation.
    public string Territory { get; set; } = string.Empty;

    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }

    public bool IsExclusive { get; set; }

    // ⚠️ No encapsulation: any code can change Status directly.
    // In the DDD version, Status can only change through Revoke() or MarkExpiredIfPastDue().
    public string Status { get; set; } = "Issued";

    public DateTimeOffset IssuedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }

    // ⚠️ Revocation fields are nullable and independently mutable.
    // Nothing prevents setting RevokedAt without setting RevocationReason.
    public string? RevocationReason { get; set; }
    public string? RevokedBy { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}
