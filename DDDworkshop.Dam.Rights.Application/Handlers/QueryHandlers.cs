namespace DDDworkshop.Dam.Rights.Application.Handlers;

using DDDworkshop.Dam.Rights.Application.Abstractions;
using DDDworkshop.Dam.Rights.Application.Dtos;
using DDDworkshop.Dam.Rights.Application.Mapping;
using DDDworkshop.Dam.Rights.Domain.Repositories;
using DDDworkshop.Dam.Rights.Domain.ValueObjects;

/// <summary>
/// Query handlers for read operations.
/// These are thin – just load and map to DTOs.
/// </summary>
public sealed class QueryHandlers
{
    private readonly ILicenseGrantRepository _grantRepo;
    private readonly IAssetRightsRepository _assetRightsRepo;
    private readonly IClock _clock;

    public QueryHandlers(
        ILicenseGrantRepository grantRepo,
        IAssetRightsRepository assetRightsRepo,
        IClock clock)
    {
        _grantRepo = grantRepo;
        _assetRightsRepo = assetRightsRepo;
        _clock = clock;
    }

    public async Task<LicenseGrantDto?> GetGrantAsync(Guid grantId, CancellationToken ct = default)
    {
        var id = new LicenseGrantId(grantId);
        var grant = await _grantRepo.GetByIdAsync(id, ct);
        return grant is null ? null : DtoMapper.ToDto(grant);
    }

    public async Task<IReadOnlyList<LicenseGrantDto>> GetGrantsForAssetAsync(Guid assetId, bool activeOnly, CancellationToken ct = default)
    {
        var id = new AssetId(assetId);
        var now = _clock.UtcNow;
        var grants = await _grantRepo.FindByAssetAsync(id, activeOnly, now, ct);
        return grants.Select(DtoMapper.ToDto).ToList();
    }

    public async Task<AssetRightsProfileDto?> GetRightsProfileAsync(Guid assetId, CancellationToken ct = default)
    {
        var id = new AssetId(assetId);
        var rights = await _assetRightsRepo.GetByIdAsync(id, ct);
        return rights is null ? null : DtoMapper.ToDto(rights);
    }
}
