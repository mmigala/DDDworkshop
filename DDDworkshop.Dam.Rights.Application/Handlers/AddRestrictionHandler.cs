namespace DDDworkshop.Dam.Rights.Application.Handlers;

using DDDworkshop.Dam.Rights.Application.Commands;
using DDDworkshop.Dam.Rights.Application.Dtos;
using DDDworkshop.Dam.Rights.Application.Mapping;
using DDDworkshop.Dam.Rights.Domain.Repositories;
using DDDworkshop.Dam.Rights.Domain.ValueObjects;

/// <summary>
/// Handles AddRestrictionCommand – adds a restriction to an asset's rights profile.
/// </summary>
public sealed class AddRestrictionHandler
{
    private readonly IAssetRightsRepository _repo;

    public AddRestrictionHandler(IAssetRightsRepository repo)
    {
        _repo = repo;
    }

    public async Task<RestrictionDto> HandleAsync(AddRestrictionCommand command, CancellationToken ct = default)
    {
        var assetId = new AssetId(command.AssetId);
        var assetRights = await _repo.GetByIdAsync(assetId, ct)
            ?? throw new InvalidOperationException($"Asset rights profile not found for asset '{command.AssetId}'.");

        var restrictedChannel = command.RestrictedChannel is not null
            ? Enum.Parse<UsageChannel>(command.RestrictedChannel, ignoreCase: true)
            : (UsageChannel?)null;

        var restrictedPurpose = command.RestrictedPurpose is not null
            ? Enum.Parse<UsagePurpose>(command.RestrictedPurpose, ignoreCase: true)
            : (UsagePurpose?)null;

        var restrictedTerritory = command.RestrictedTerritoryCodes is not null
            ? new Territory(command.RestrictedTerritoryCodes)
            : null;

        var requiresRelease = command.RequiresRelease is not null
            ? Enum.Parse<ReleaseStatus>(command.RequiresRelease, ignoreCase: true)
            : (ReleaseStatus?)null;

        // Delegate to the aggregate – it enforces invariants
        var restriction = assetRights.AddRestriction(
            command.Description,
            restrictedChannel,
            restrictedPurpose,
            restrictedTerritory,
            requiresRelease);

        await _repo.SaveAsync(assetRights, ct);

        return DtoMapper.ToDto(restriction);
    }
}
