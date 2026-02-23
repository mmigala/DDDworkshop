namespace DDDworkshop.Dam.Rights.Domain.ValueObjects;

using SeedWork;

/// <summary>
/// The reason a license grant was revoked.
/// Immutable value object capturing the reason text and who requested the revocation.
/// </summary>
public sealed class RevocationReason : ValueObject
{
    public string Reason { get; }
    public string RevokedBy { get; }
    public DateTimeOffset RevokedAt { get; }

    public RevocationReason(string reason, string revokedBy, DateTimeOffset revokedAt)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Revocation reason cannot be empty.", nameof(reason));

        if (string.IsNullOrWhiteSpace(revokedBy))
            throw new ArgumentException("RevokedBy cannot be empty.", nameof(revokedBy));

        Reason = reason;
        RevokedBy = revokedBy;
        RevokedAt = revokedAt;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Reason;
        yield return RevokedBy;
        yield return RevokedAt;
    }

    public override string ToString() => $"Revoked by {RevokedBy}: {Reason}";
}
