namespace DDDworkshop.Dam.Rights.Application.Handlers;

using DDDworkshop.Dam.Rights.Application.Abstractions;
using DDDworkshop.Dam.Rights.Application.Commands;
using DDDworkshop.Dam.Rights.Application.Dtos;
using DDDworkshop.Dam.Rights.Application.Mapping;
using DDDworkshop.Dam.Rights.Domain.Aggregates.LicenseGrantAggregate;
using DDDworkshop.Dam.Rights.Domain.Policies;
using DDDworkshop.Dam.Rights.Domain.Repositories;
using DDDworkshop.Dam.Rights.Domain.ValueObjects;

/// <summary>
/// Handles RequestLicenseCommand.
/// 
/// This is a thin orchestrator – it coordinates domain objects but contains
/// NO business logic itself. All rules live in the aggregates and policies.
/// 
/// Steps:
///   1. Load the AssetRights aggregate
///   2. Build domain value objects from the command
///   3. Evaluate rights (aggregate method)
///   4. If exclusive, check the exclusivity policy (domain service)
///   5. If allowed, issue the grant (factory method on LicenseGrant)
///   6. If exclusive, reserve the scope on AssetRights
///   7. Save and dispatch events
/// </summary>
public sealed class RequestLicenseHandler
{
    private readonly IAssetRightsRepository _assetRightsRepo;
    private readonly ILicenseGrantRepository _grantRepo;
    private readonly IExclusiveLicensingPolicy _exclusivityPolicy;
    private readonly IClock _clock;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public RequestLicenseHandler(
        IAssetRightsRepository assetRightsRepo,
        ILicenseGrantRepository grantRepo,
        IExclusiveLicensingPolicy exclusivityPolicy,
        IClock clock,
        IDomainEventDispatcher eventDispatcher)
    {
        _assetRightsRepo = assetRightsRepo;
        _grantRepo = grantRepo;
        _exclusivityPolicy = exclusivityPolicy;
        _clock = clock;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<RightsDecisionDto> HandleAsync(RequestLicenseCommand command, CancellationToken ct = default)
    {
        var now = _clock.UtcNow;

        // 1. Load asset rights
        var assetId = new AssetId(command.AssetId);
        var assetRights = await _assetRightsRepo.GetByIdAsync(assetId, ct)
            ?? throw new InvalidOperationException($"Asset rights profile not found for asset '{command.AssetId}'.");

        // 2. Build domain value objects
        var channel = Enum.Parse<UsageChannel>(command.Channel, ignoreCase: true);
        var purpose = Enum.Parse<UsagePurpose>(command.Purpose, ignoreCase: true);
        var territory = new Territory(command.TerritoryCodes);
        var timeWindow = new TimeWindow(command.TimeWindowStart, command.TimeWindowEnd);
        var scope = new LicenseScope(channel, territory, timeWindow, purpose);
        var terms = new LicenseTerms(scope, command.IsExclusive);

        // 3. Evaluate rights against the aggregate
        var decision = assetRights.Evaluate(terms);
        if (!decision.IsAllowed)
            return new RightsDecisionDto(false, null, decision.DenialReasons);

        // 4. If exclusive, check the cross-aggregate exclusivity policy
        if (command.IsExclusive)
        {
            var exclusivityCheck = await _exclusivityPolicy.CheckAsync(assetId, scope, now, ct);
            if (exclusivityCheck.HasConflict)
                return new RightsDecisionDto(false, null, [exclusivityCheck.ConflictDescription!]);
        }

        // 5. Issue the grant (domain factory method)
        var licenseeId = new LicenseeId(command.LicenseeId);
        var grant = LicenseGrant.Issue(assetId, licenseeId, terms, now);

        // 6. If exclusive, reserve the scope on the asset's rights profile
        if (command.IsExclusive)
        {
            assetRights.ReserveExclusiveScope(grant.Id, scope);
            await _assetRightsRepo.SaveAsync(assetRights, ct);
        }

        // 7. Save the grant and dispatch domain events
        await _grantRepo.SaveAsync(grant, ct);
        await _eventDispatcher.DispatchAsync(grant.DomainEvents, ct);
        grant.ClearDomainEvents();

        return new RightsDecisionDto(true, grant.Id.Value, []);
    }
}
