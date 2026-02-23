namespace DDDworkshop.Dam.Rights.Domain.Aggregates.LicenseGrantAggregate;

/// <summary>
/// The lifecycle states of a license grant.
/// </summary>
public enum GrantStatus
{
    /// <summary>Grant has been issued and is currently active.</summary>
    Issued,

    /// <summary>Grant has been revoked before its natural expiry.</summary>
    Revoked,

    /// <summary>Grant has expired (past its end date).</summary>
    Expired
}
