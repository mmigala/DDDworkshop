namespace DDDworkshop.Dam.Rights.Domain.Policies;

using DDDworkshop.Dam.Rights.Domain.Repositories;
using DDDworkshop.Dam.Rights.Domain.ValueObjects;

/// <summary>
/// Domain service: ExclusiveLicensingPolicy
/// 
/// Checks whether a requested exclusive scope conflicts with any existing
/// active grants for the same asset. This is cross-aggregate logic:
///   - It reads from the LicenseGrant repository (existing grants)
///   - It applies domain rules (scope overlap detection)
///   - It doesn't belong to a single aggregate
/// 
/// The aggregate doesn't query the DB itself – the application layer
/// calls this policy and feeds the result into the aggregate. This is isolation.
/// </summary>
public sealed class ExclusiveLicensingPolicy : IExclusiveLicensingPolicy
{
    private readonly ILicenseGrantRepository _grantRepository;

    public ExclusiveLicensingPolicy(ILicenseGrantRepository grantRepository)
    {
        _grantRepository = grantRepository ?? throw new ArgumentNullException(nameof(grantRepository));
    }

    public async Task<ExclusivityCheckResult> CheckAsync(
        AssetId assetId,
        LicenseScope scope,
        DateTimeOffset now,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(assetId);
        ArgumentNullException.ThrowIfNull(scope);

        var activeGrants = await _grantRepository.FindActiveByAssetAsync(assetId, now, ct);

        // Check if any active grant with exclusive terms overlaps the requested scope
        foreach (var grant in activeGrants)
        {
            if (!grant.Terms.IsExclusive)
                continue;

            if (grant.Terms.Scope.OverlapsWith(scope))
            {
                return ExclusivityCheckResult.Conflict(
                    $"ExclusiveConflict: active exclusive grant '{grant.Id}' " +
                    $"for licensee '{grant.LicenseeId}' overlaps the requested scope " +
                    $"({grant.Terms.Scope}).");
            }
        }

        return ExclusivityCheckResult.NoConflict();
    }
}
