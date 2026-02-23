namespace DDDworkshop.Dam.Rights.Domain.Repositories;

using DDDworkshop.Dam.Rights.Domain.Aggregates.LicenseGrantAggregate;
using DDDworkshop.Dam.Rights.Domain.ValueObjects;

/// <summary>
/// Repository abstraction for the LicenseGrant aggregate.
/// Defined in the Domain layer – implementations live in Infrastructure.
/// 
/// Includes FindActiveByAssetAsync which is needed by the ExclusiveLicensingPolicy
/// domain service to check for scope conflicts across grants.
/// </summary>
public interface ILicenseGrantRepository
{
    Task<LicenseGrant?> GetByIdAsync(LicenseGrantId grantId, CancellationToken ct = default);
    Task<IReadOnlyList<LicenseGrant>> FindByAssetAsync(AssetId assetId, bool activeOnly, DateTimeOffset now, CancellationToken ct = default);
    Task<IReadOnlyList<LicenseGrant>> FindActiveByAssetAsync(AssetId assetId, DateTimeOffset now, CancellationToken ct = default);
    Task SaveAsync(LicenseGrant grant, CancellationToken ct = default);
}
