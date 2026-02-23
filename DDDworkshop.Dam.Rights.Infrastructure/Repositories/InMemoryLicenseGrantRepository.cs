namespace DDDworkshop.Dam.Rights.Infrastructure.Repositories;

using System.Collections.Concurrent;
using DDDworkshop.Dam.Rights.Domain.Aggregates.LicenseGrantAggregate;
using DDDworkshop.Dam.Rights.Domain.Repositories;
using DDDworkshop.Dam.Rights.Domain.ValueObjects;

/// <summary>
/// In-memory implementation of ILicenseGrantRepository.
/// Backed by a ConcurrentDictionary – data resets on app restart.
/// </summary>
public sealed class InMemoryLicenseGrantRepository : ILicenseGrantRepository
{
    private readonly ConcurrentDictionary<LicenseGrantId, LicenseGrant> _store = new();

    public Task<LicenseGrant?> GetByIdAsync(LicenseGrantId grantId, CancellationToken ct = default)
    {
        _store.TryGetValue(grantId, out var result);
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<LicenseGrant>> FindByAssetAsync(
        AssetId assetId, bool activeOnly, DateTimeOffset now, CancellationToken ct = default)
    {
        var grants = _store.Values
            .Where(g => g.AssetId == assetId)
            .Where(g => !activeOnly || g.IsActive(now))
            .ToList();

        return Task.FromResult<IReadOnlyList<LicenseGrant>>(grants);
    }

    public Task<IReadOnlyList<LicenseGrant>> FindActiveByAssetAsync(
        AssetId assetId, DateTimeOffset now, CancellationToken ct = default)
    {
        return FindByAssetAsync(assetId, activeOnly: true, now, ct);
    }

    public Task SaveAsync(LicenseGrant grant, CancellationToken ct = default)
    {
        _store[grant.Id] = grant;
        return Task.CompletedTask;
    }
}
