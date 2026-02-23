namespace DDDworkshop.Dam.Rights.Application.Handlers;

using DDDworkshop.Dam.Rights.Application.Commands;
using DDDworkshop.Dam.Rights.Application.Dtos;
using DDDworkshop.Dam.Rights.Application.Mapping;
using DDDworkshop.Dam.Rights.Domain.Repositories;
using DDDworkshop.Dam.Rights.Domain.ValueObjects;

/// <summary>
/// Handles AddExclusiveWindowCommand – reserves an exclusive scope on an asset's rights profile.
/// </summary>
public sealed class AddExclusiveWindowHandler
{
    private readonly IAssetRightsRepository _repo;

    public AddExclusiveWindowHandler(IAssetRightsRepository repo)
    {
        _repo = repo;
    }

    public async Task<ExclusiveWindowDto> HandleAsync(AddExclusiveWindowCommand command, CancellationToken ct = default)
    {
        var assetId = new AssetId(command.AssetId);
        var assetRights = await _repo.GetByIdAsync(assetId, ct)
            ?? throw new InvalidOperationException($"Asset rights profile not found for asset '{command.AssetId}'.");

        var channel = Enum.Parse<UsageChannel>(command.Channel, ignoreCase: true);
        var purpose = Enum.Parse<UsagePurpose>(command.Purpose, ignoreCase: true);
        var territory = new Territory(command.TerritoryCodes);
        var timeWindow = new TimeWindow(command.TimeWindowStart, command.TimeWindowEnd);
        var scope = new LicenseScope(channel, territory, timeWindow, purpose);
        var grantId = new LicenseGrantId(command.GrantId);

        // Delegate to aggregate – it enforces no-overlap invariant
        assetRights.ReserveExclusiveScope(grantId, scope);

        await _repo.SaveAsync(assetRights, ct);

        // Return the newly added window
        var window = assetRights.ExclusiveWindows.Last();
        return DtoMapper.ToDto(window);
    }
}
