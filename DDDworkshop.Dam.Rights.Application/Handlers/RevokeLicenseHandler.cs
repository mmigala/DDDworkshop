namespace DDDworkshop.Dam.Rights.Application.Handlers;

using DDDworkshop.Dam.Rights.Application.Abstractions;
using DDDworkshop.Dam.Rights.Application.Commands;
using DDDworkshop.Dam.Rights.Domain.Repositories;
using DDDworkshop.Dam.Rights.Domain.ValueObjects;

/// <summary>
/// Handles RevokeLicenseCommand.
/// 
/// Thin orchestrator: loads the grant, delegates to the aggregate's Revoke method,
/// handles exclusive scope cleanup, saves, and dispatches events.
/// </summary>
public sealed class RevokeLicenseHandler
{
    private readonly ILicenseGrantRepository _grantRepo;
    private readonly IAssetRightsRepository _assetRightsRepo;
    private readonly IClock _clock;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public RevokeLicenseHandler(
        ILicenseGrantRepository grantRepo,
        IAssetRightsRepository assetRightsRepo,
        IClock clock,
        IDomainEventDispatcher eventDispatcher)
    {
        _grantRepo = grantRepo;
        _assetRightsRepo = assetRightsRepo;
        _clock = clock;
        _eventDispatcher = eventDispatcher;
    }

    public async Task HandleAsync(RevokeLicenseCommand command, CancellationToken ct = default)
    {
        var now = _clock.UtcNow;

        // 1. Load the grant
        var grantId = new LicenseGrantId(command.GrantId);
        var grant = await _grantRepo.GetByIdAsync(grantId, ct)
            ?? throw new InvalidOperationException($"License grant '{command.GrantId}' not found.");

        // 2. Revoke (domain method enforces invariants)
        grant.Revoke(command.Reason, command.RevokedBy, now);

        // 3. If the grant was exclusive, release the scope on the asset's rights profile
        if (grant.Terms.IsExclusive)
        {
            var assetRights = await _assetRightsRepo.GetByIdAsync(grant.AssetId, ct);
            if (assetRights is not null)
            {
                assetRights.RevokeExclusiveScope(grant.Id);
                await _assetRightsRepo.SaveAsync(assetRights, ct);
            }
        }

        // 4. Save and dispatch events
        await _grantRepo.SaveAsync(grant, ct);
        await _eventDispatcher.DispatchAsync(grant.DomainEvents, ct);
        grant.ClearDomainEvents();
    }
}
