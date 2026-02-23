namespace DDDworkshop.Dam.Rights.Domain.Events;

using DDDworkshop.Dam.Rights.Domain.SeedWork;
using DDDworkshop.Dam.Rights.Domain.ValueObjects;

/// <summary>
/// Domain event raised when a license grant is issued.
/// 
/// Handlers in the Application/Infrastructure layer can react to this event
/// (e.g., update search index, notify downstream systems, create watermarking jobs).
/// This decouples the domain from side effects.
/// </summary>
public sealed class LicenseGrantedEvent : IDomainEvent
{
    public AssetId AssetId { get; }
    public LicenseGrantId GrantId { get; }
    public LicenseTerms Terms { get; }
    public LicenseeId LicenseeId { get; }
    public DateTime OccurredOn { get; }

    public LicenseGrantedEvent(AssetId assetId, LicenseGrantId grantId, LicenseTerms terms, LicenseeId licenseeId)
    {
        AssetId = assetId;
        GrantId = grantId;
        Terms = terms;
        LicenseeId = licenseeId;
        OccurredOn = DateTime.UtcNow;
    }
}
