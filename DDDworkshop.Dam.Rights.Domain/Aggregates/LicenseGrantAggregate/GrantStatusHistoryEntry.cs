namespace DDDworkshop.Dam.Rights.Domain.Aggregates.LicenseGrantAggregate;

using DDDworkshop.Dam.Rights.Domain.SeedWork;

/// <summary>
/// Entity within the LicenseGrant aggregate.
/// Records a status transition with a timestamp, providing a full audit trail
/// of the grant's lifecycle (Issued → Revoked, Issued → Expired).
/// </summary>
public sealed class GrantStatusHistoryEntry : Entity<Guid>
{
    /// <summary>The status that was transitioned to.</summary>
    public GrantStatus Status { get; }

    /// <summary>When this transition occurred.</summary>
    public DateTimeOffset OccurredAt { get; }

    /// <summary>Optional note about the transition (e.g., revocation reason).</summary>
    public string? Note { get; }

    public GrantStatusHistoryEntry(Guid id, GrantStatus status, DateTimeOffset occurredAt, string? note = null)
        : base(id)
    {
        Status = status;
        OccurredAt = occurredAt;
        Note = note;
    }
}
