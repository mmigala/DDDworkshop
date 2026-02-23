namespace DDDworkshop.Dam.Rights.Infrastructure.Repositories;

using System.Collections.Concurrent;
using DDDworkshop.Dam.Rights.Domain.Aggregates.AssetRightsAggregate;
using DDDworkshop.Dam.Rights.Domain.Repositories;
using DDDworkshop.Dam.Rights.Domain.ValueObjects;

/// <summary>
/// In-memory implementation of IAssetRightsRepository.
/// Backed by a ConcurrentDictionary – data resets on app restart.
/// 
/// This demonstrates that the domain and application layers are completely
/// independent of persistence technology. Swap this for EF Core, Cosmos DB,
/// etc. without changing a single line of domain code.
/// </summary>
public sealed class InMemoryAssetRightsRepository : IAssetRightsRepository
{
    private readonly ConcurrentDictionary<AssetId, AssetRights> _store = new();

    public Task<AssetRights?> GetByIdAsync(AssetId assetId, CancellationToken ct = default)
    {
        _store.TryGetValue(assetId, out var result);
        return Task.FromResult(result);
    }

    public Task SaveAsync(AssetRights assetRights, CancellationToken ct = default)
    {
        _store[assetRights.Id] = assetRights;
        return Task.CompletedTask;
    }
}
