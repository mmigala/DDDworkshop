namespace DDDworkshop.Dam.Rights.Application.Mapping;

using DDDworkshop.Dam.Rights.Application.Dtos;
using DDDworkshop.Dam.Rights.Domain.Aggregates.AssetRightsAggregate;
using DDDworkshop.Dam.Rights.Domain.Aggregates.LicenseGrantAggregate;

/// <summary>
/// Maps domain objects to application DTOs.
/// This mapping lives in the Application layer – domain objects never reference DTOs.
/// </summary>
public static class DtoMapper
{
    public static LicenseGrantDto ToDto(LicenseGrant grant)
    {
        return new LicenseGrantDto(
            GrantId: grant.Id.Value,
            AssetId: grant.AssetId.Value,
            LicenseeId: grant.LicenseeId.Value,
            Channel: grant.Terms.Scope.Channel.ToString(),
            TerritoryCodes: grant.Terms.Scope.Territory.CountryCodes.ToList(),
            TimeWindowStart: grant.Terms.Scope.TimeWindow.Start,
            TimeWindowEnd: grant.Terms.Scope.TimeWindow.End,
            Purpose: grant.Terms.Scope.Purpose.ToString(),
            IsExclusive: grant.Terms.IsExclusive,
            Status: grant.Status.ToString(),
            IssuedAt: grant.IssuedAt,
            ExpiresAt: grant.ExpiresAt,
            RevocationReason: grant.Revocation?.Reason,
            RevokedBy: grant.Revocation?.RevokedBy,
            RevokedAt: grant.Revocation?.RevokedAt);
    }

    public static AssetRightsProfileDto ToDto(AssetRights assetRights)
    {
        return new AssetRightsProfileDto(
            AssetId: assetRights.Id.Value,
            OwnerId: assetRights.OwnerId.Value,
            ReleaseStatus: assetRights.ReleaseStatus.ToString(),
            Restrictions: assetRights.Restrictions.Select(ToDto).ToList(),
            ExclusiveWindows: assetRights.ExclusiveWindows.Select(ToDto).ToList());
    }

    public static RestrictionDto ToDto(RightRestriction restriction)
    {
        return new RestrictionDto(
            Id: restriction.Id,
            Description: restriction.Description,
            RestrictedChannel: restriction.RestrictedChannel?.ToString(),
            RestrictedPurpose: restriction.RestrictedPurpose?.ToString(),
            RestrictedTerritoryCodes: restriction.RestrictedTerritory?.CountryCodes.ToList(),
            RequiresRelease: restriction.RequiresRelease?.ToString());
    }

    public static ExclusiveWindowDto ToDto(ExclusiveWindow window)
    {
        return new ExclusiveWindowDto(
            Id: window.Id,
            GrantId: window.GrantId.Value,
            Channel: window.Scope.Channel.ToString(),
            TerritoryCodes: window.Scope.Territory.CountryCodes.ToList(),
            TimeWindowStart: window.Scope.TimeWindow.Start,
            TimeWindowEnd: window.Scope.TimeWindow.End,
            Purpose: window.Scope.Purpose.ToString());
    }
}
