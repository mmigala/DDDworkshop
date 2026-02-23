namespace DDDworkshop.Dam.Rights.Api.Mapping;

using DDDworkshop.Dam.Rights.Application.Dtos;
using DDDworkshop.Dam.Rights.Api.Models.Responses;

/// <summary>
/// Maps application DTOs to API response models.
/// 
/// This extra mapping layer keeps the API contract stable even if
/// internal DTOs change — another benefit of layered architecture.
/// </summary>
public static class ResponseMapper
{
    public static RightsDecisionResponse ToResponse(RightsDecisionDto dto) => new()
    {
        IsAllowed = dto.IsAllowed,
        GrantId = dto.GrantId,
        DenialReasons = dto.DenialReasons
    };

    public static LicenseGrantResponse ToResponse(LicenseGrantDto dto) => new()
    {
        GrantId = dto.GrantId,
        AssetId = dto.AssetId,
        LicenseeId = dto.LicenseeId,
        Channel = dto.Channel,
        TerritoryCodes = dto.TerritoryCodes,
        TimeWindowStart = dto.TimeWindowStart,
        TimeWindowEnd = dto.TimeWindowEnd,
        Purpose = dto.Purpose,
        IsExclusive = dto.IsExclusive,
        Status = dto.Status,
        IssuedAt = dto.IssuedAt,
        ExpiresAt = dto.ExpiresAt,
        RevocationReason = dto.RevocationReason,
        RevokedBy = dto.RevokedBy,
        RevokedAt = dto.RevokedAt
    };

    public static AssetRightsProfileResponse ToResponse(AssetRightsProfileDto dto) => new()
    {
        AssetId = dto.AssetId,
        OwnerId = dto.OwnerId,
        ReleaseStatus = dto.ReleaseStatus,
        Restrictions = dto.Restrictions.Select(ToResponse).ToList(),
        ExclusiveWindows = dto.ExclusiveWindows.Select(ToResponse).ToList()
    };

    public static RestrictionResponse ToResponse(RestrictionDto dto) => new()
    {
        Id = dto.Id,
        Description = dto.Description,
        RestrictedChannel = dto.RestrictedChannel,
        RestrictedPurpose = dto.RestrictedPurpose,
        RestrictedTerritoryCodes = dto.RestrictedTerritoryCodes,
        RequiresRelease = dto.RequiresRelease
    };

    public static ExclusiveWindowResponse ToResponse(ExclusiveWindowDto dto) => new()
    {
        Id = dto.Id,
        GrantId = dto.GrantId,
        Channel = dto.Channel,
        TerritoryCodes = dto.TerritoryCodes,
        TimeWindowStart = dto.TimeWindowStart,
        TimeWindowEnd = dto.TimeWindowEnd,
        Purpose = dto.Purpose
    };
}
