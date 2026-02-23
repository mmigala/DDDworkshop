namespace DDDworkshop.Dam.Rights.Domain.Repositories;

using DDDworkshop.Dam.Rights.Domain.Aggregates.AssetRightsAggregate;
using DDDworkshop.Dam.Rights.Domain.ValueObjects;

/// <summary>
/// Repository abstraction for the AssetRights aggregate.
/// Defined in the Domain layer – implementations live in Infrastructure.
/// This is how we achieve isolation: domain logic never depends on persistence details.
/// </summary>
public interface IAssetRightsRepository
{
    Task<AssetRights?> GetByIdAsync(AssetId assetId, CancellationToken ct = default);
    Task SaveAsync(AssetRights assetRights, CancellationToken ct = default);
}
