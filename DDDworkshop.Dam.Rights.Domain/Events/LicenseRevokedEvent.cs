namespace DDDworkshop.Dam.Rights.Domain.Events;

using DDDworkshop.Dam.Rights.Domain.SeedWork;
using DDDworkshop.Dam.Rights.Domain.ValueObjects;

/// <summary>
/// Domain event raised when a license grant is revoked.
/// 
/// Downstream handlers can react to this (e.g., remove watermark authorization,
/// log for compliance, notify the licensee).
/// </summary>
public sealed class LicenseRevokedEvent : IDomainEvent
{
    public LicenseGrantId GrantId { get; }
    public AssetId AssetId { get; }
    public string Reason { get; }
    public string RevokedBy { get; }
    public DateTime OccurredOn { get; }

    public LicenseRevokedEvent(LicenseGrantId grantId, AssetId assetId, string reason, string revokedBy)
    {
        GrantId = grantId;
        AssetId = assetId;
        Reason = reason;
        RevokedBy = revokedBy;
        OccurredOn = DateTime.UtcNow;
    }
}
