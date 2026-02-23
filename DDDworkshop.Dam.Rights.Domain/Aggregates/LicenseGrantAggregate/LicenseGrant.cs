namespace DDDworkshop.Dam.Rights.Domain.Aggregates.LicenseGrantAggregate;

using DDDworkshop.Dam.Rights.Domain.Events;
using DDDworkshop.Dam.Rights.Domain.Exceptions;
using DDDworkshop.Dam.Rights.Domain.SeedWork;
using DDDworkshop.Dam.Rights.Domain.ValueObjects;

/// <summary>
/// Aggregate Root: LicenseGrant
/// 
/// Represents an issued license – an auditable "permission document" that records
/// what usage was granted, to whom, for what scope and time period.
/// 
/// Lifecycle: Issued → Active → Expired (natural) or Revoked (manual).
/// 
/// Key invariants:
///   - Terms are immutable once issued (change = revoke + issue new).
///   - Cannot revoke an already expired grant.
///   - Cannot issue if already issued.
///   - State transitions are tracked in a full audit history.
/// </summary>
public sealed class LicenseGrant : AggregateRoot<LicenseGrantId>
{
    private readonly List<GrantStatusHistoryEntry> _statusHistory = [];

    /// <summary>The asset this license applies to.</summary>
    public AssetId AssetId { get; }

    /// <summary>The customer who holds the license.</summary>
    public LicenseeId LicenseeId { get; }

    /// <summary>The immutable terms of the license (scope + exclusivity).</summary>
    public LicenseTerms Terms { get; }

    /// <summary>When the grant was issued.</summary>
    public DateTimeOffset IssuedAt { get; }

    /// <summary>When the grant expires.</summary>
    public DateTimeOffset ExpiresAt { get; }

    /// <summary>Current status of the grant.</summary>
    public GrantStatus Status { get; private set; }

    /// <summary>Revocation details, if the grant was revoked.</summary>
    public RevocationReason? Revocation { get; private set; }

    /// <summary>Full audit trail of status transitions.</summary>
    public IReadOnlyList<GrantStatusHistoryEntry> StatusHistory => _statusHistory.AsReadOnly();

    // Private constructor – use the Issue factory method
    private LicenseGrant(
        LicenseGrantId id,
        AssetId assetId,
        LicenseeId licenseeId,
        LicenseTerms terms,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt)
        : base(id)
    {
        AssetId = assetId;
        LicenseeId = licenseeId;
        Terms = terms;
        IssuedAt = issuedAt;
        ExpiresAt = expiresAt;
        Status = GrantStatus.Issued;
    }

    // ──────────────────────────────────────────────
    //  Factory Method
    // ──────────────────────────────────────────────

    /// <summary>
    /// Issues a new license grant. This is the only way to create a LicenseGrant.
    /// 
    /// The factory method enforces that all required data is present and
    /// that the initial state is always consistent.
    /// </summary>
    public static LicenseGrant Issue(
        AssetId assetId,
        LicenseeId licenseeId,
        LicenseTerms terms,
        DateTimeOffset issuedAt)
    {
        ArgumentNullException.ThrowIfNull(assetId);
        ArgumentNullException.ThrowIfNull(licenseeId);
        ArgumentNullException.ThrowIfNull(terms);

        var expiresAt = terms.Scope.TimeWindow.End;

        if (issuedAt >= expiresAt)
            throw new InvalidTimeWindowException(issuedAt, expiresAt);

        var grant = new LicenseGrant(
            LicenseGrantId.New(),
            assetId,
            licenseeId,
            terms,
            issuedAt,
            expiresAt);

        grant.RecordStatusTransition(GrantStatus.Issued, issuedAt, "License issued");
        grant.AddDomainEvent(new LicenseGrantedEvent(assetId, grant.Id, terms, licenseeId));

        return grant;
    }

    // ──────────────────────────────────────────────
    //  State Transitions
    // ──────────────────────────────────────────────

    /// <summary>
    /// Revokes this license grant.
    /// 
    /// Invariant: cannot revoke an already expired or revoked grant.
    /// </summary>
    public void Revoke(string reason, string revokedBy, DateTimeOffset now)
    {
        if (Status == GrantStatus.Expired)
            throw new DomainException("Cannot revoke an expired grant.");

        if (Status == GrantStatus.Revoked)
            throw new DomainException("Grant is already revoked.");

        Revocation = new RevocationReason(reason, revokedBy, now);
        Status = GrantStatus.Revoked;

        RecordStatusTransition(GrantStatus.Revoked, now, $"Revoked by {revokedBy}: {reason}");
        AddDomainEvent(new LicenseRevokedEvent(Id, AssetId, reason, revokedBy));
    }

    /// <summary>
    /// Marks this grant as expired if the current time is past the expiry date.
    /// 
    /// This is a natural lifecycle transition, not an error.
    /// </summary>
    public void MarkExpiredIfPastDue(DateTimeOffset now)
    {
        if (Status != GrantStatus.Issued)
            return; // Already revoked or expired, nothing to do

        if (now >= ExpiresAt)
        {
            Status = GrantStatus.Expired;
            RecordStatusTransition(GrantStatus.Expired, now, "Grant expired naturally");
        }
    }

    /// <summary>
    /// Returns true if this grant is currently active (issued and not yet expired or revoked).
    /// </summary>
    public bool IsActive(DateTimeOffset now)
    {
        return Status == GrantStatus.Issued && now < ExpiresAt;
    }

    // ──────────────────────────────────────────────
    //  Private Helpers
    // ──────────────────────────────────────────────

    private void RecordStatusTransition(GrantStatus status, DateTimeOffset occurredAt, string? note)
    {
        _statusHistory.Add(new GrantStatusHistoryEntry(Guid.NewGuid(), status, occurredAt, note));
    }

    // ──────────────────────────────────────────────
    //  Nested domain exception for conciseness
    // ──────────────────────────────────────────────

    /// <summary>
    /// Domain exception specific to LicenseGrant state violations.
    /// </summary>
    public sealed class DomainException : Exceptions.DomainException
    {
        public DomainException(string message) : base(message) { }
    }
}
