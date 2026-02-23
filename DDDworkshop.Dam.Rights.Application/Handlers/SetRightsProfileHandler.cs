namespace DDDworkshop.Dam.Rights.Application.Handlers;

using DDDworkshop.Dam.Rights.Application.Commands;
using DDDworkshop.Dam.Rights.Application.Dtos;
using DDDworkshop.Dam.Rights.Application.Mapping;
using DDDworkshop.Dam.Rights.Domain.Aggregates.AssetRightsAggregate;
using DDDworkshop.Dam.Rights.Domain.Repositories;
using DDDworkshop.Dam.Rights.Domain.ValueObjects;

/// <summary>
/// Handles SetRightsProfileCommand – creates or updates an asset's rights profile.
/// </summary>
public sealed class SetRightsProfileHandler
{
    private readonly IAssetRightsRepository _repo;

    public SetRightsProfileHandler(IAssetRightsRepository repo)
    {
        _repo = repo;
    }

    public async Task<AssetRightsProfileDto> HandleAsync(SetRightsProfileCommand command, CancellationToken ct = default)
    {
        var assetId = new AssetId(command.AssetId);
        var ownerId = new OwnerId(command.OwnerId);
        var releaseStatus = Enum.Parse<ReleaseStatus>(command.ReleaseStatus, ignoreCase: true);

        var assetRights = await _repo.GetByIdAsync(assetId, ct);

        if (assetRights is null)
        {
            // Create new rights profile
            assetRights = new AssetRights(assetId, ownerId, releaseStatus);
        }
        else
        {
            // Update existing profile
            assetRights.UpdateProfile(ownerId, releaseStatus);
        }

        await _repo.SaveAsync(assetRights, ct);

        return DtoMapper.ToDto(assetRights);
    }
}
