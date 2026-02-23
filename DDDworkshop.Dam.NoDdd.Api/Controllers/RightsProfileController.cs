namespace DDDworkshop.Dam.NoDdd.Api.Controllers;

using DDDworkshop.Dam.NoDdd.Api.Services;
using Microsoft.AspNetCore.Mvc;

// ⚠️ ANTI-PATTERN: Controller directly calls "god service" and returns raw entities.
// No layering, no DTOs, no separation of read/write concerns.

[ApiController]
[Route("assets/{assetId:guid}/rights-profile")]
[Produces("application/json")]
public class RightsProfileController : ControllerBase
{
    private readonly RightsService _rightsService;

    public RightsProfileController(RightsService rightsService)
    {
        _rightsService = rightsService;
    }

    /// <summary>
    /// Create or update an asset's rights profile.
    /// </summary>
    [HttpPut]
    public IActionResult SetProfile(
        [FromRoute] Guid assetId,
        [FromBody] SetProfileBody body)
    {
        var asset = _rightsService.SetRightsProfile(assetId, body.OwnerId, body.ReleaseStatus);
        var restrictions = _rightsService.GetRestrictions(assetId);
        var windows = _rightsService.GetExclusiveWindows(assetId);

        return Ok(ToProfileResponse(asset, restrictions, windows));
    }

    /// <summary>
    /// Get an asset's rights profile.
    /// </summary>
    [HttpGet]
    public IActionResult GetProfile([FromRoute] Guid assetId)
    {
        var asset = _rightsService.GetAsset(assetId);
        if (asset is null)
            return NotFound(new { error = $"Rights profile not found for asset '{assetId}'." });

        var restrictions = _rightsService.GetRestrictions(assetId);
        var windows = _rightsService.GetExclusiveWindows(assetId);

        return Ok(ToProfileResponse(asset, restrictions, windows));
    }

    /// <summary>
    /// Add a restriction to an asset's rights profile.
    /// </summary>
    [HttpPost("restrictions")]
    public IActionResult AddRestriction(
        [FromRoute] Guid assetId,
        [FromBody] AddRestrictionBody body)
    {
        try
        {
            // ⚠️ Territory codes joined into a flat string — lossy representation.
            var territory = body.RestrictedTerritoryCodes is not null
                ? string.Join(",", body.RestrictedTerritoryCodes)
                : null;

            var restriction = _rightsService.AddRestriction(
                assetId,
                body.Description,
                body.RestrictedChannel,
                body.RestrictedPurpose,
                territory,
                body.RequiresRelease);

            return Created($"/assets/{assetId}/rights-profile", new
            {
                restriction.Id,
                restriction.Description,
                restrictedChannel = restriction.Channel,
                restrictedPurpose = restriction.Purpose,
                restrictedTerritoryCodes = restriction.Territory?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                requiresRelease = restriction.RequiresRelease
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Add an exclusive window to an asset's rights profile.
    /// </summary>
    [HttpPost("exclusive-windows")]
    public IActionResult AddExclusiveWindow(
        [FromRoute] Guid assetId,
        [FromBody] AddExclusiveWindowBody body)
    {
        try
        {
            var territory = string.Join(",", body.TerritoryCodes);

            var window = _rightsService.AddExclusiveWindow(
                assetId,
                body.GrantId,
                body.Channel,
                territory,
                body.TimeWindowStart,
                body.TimeWindowEnd,
                body.Purpose);

            return Created($"/assets/{assetId}/rights-profile", new
            {
                window.Id,
                window.GrantId,
                window.Channel,
                territoryCodes = window.Territory.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                timeWindowStart = window.Start,
                timeWindowEnd = window.End,
                window.Purpose
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // ⚠️ Inline mapping — no reusable mapper, no DTO layer.
    private static object ToProfileResponse(
        Entities.AssetEntity asset,
        List<Entities.RestrictionEntity> restrictions,
        List<Entities.ExclusiveWindowEntity> windows)
    {
        return new
        {
            assetId = asset.Id,
            ownerId = asset.OwnerId,
            releaseStatus = asset.ReleaseStatus,
            restrictions = restrictions.Select(r => new
            {
                r.Id,
                r.Description,
                restrictedChannel = r.Channel,
                restrictedPurpose = r.Purpose,
                restrictedTerritoryCodes = r.Territory?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                requiresRelease = r.RequiresRelease
            }),
            exclusiveWindows = windows.Select(w => new
            {
                w.Id,
                w.GrantId,
                w.Channel,
                territoryCodes = w.Territory.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                timeWindowStart = w.Start,
                timeWindowEnd = w.End,
                w.Purpose
            })
        };
    }
}

// ⚠️ Request body classes scattered alongside controller — no organization.
public class SetProfileBody
{
    public Guid OwnerId { get; set; }
    public string ReleaseStatus { get; set; } = default!;
}

public class AddRestrictionBody
{
    public string Description { get; set; } = default!;
    public string? RestrictedChannel { get; set; }
    public string? RestrictedPurpose { get; set; }
    public List<string>? RestrictedTerritoryCodes { get; set; }
    public string? RequiresRelease { get; set; }
}

public class AddExclusiveWindowBody
{
    public Guid GrantId { get; set; }
    public string Channel { get; set; } = default!;
    public List<string> TerritoryCodes { get; set; } = [];
    public DateTimeOffset TimeWindowStart { get; set; }
    public DateTimeOffset TimeWindowEnd { get; set; }
    public string Purpose { get; set; } = default!;
}
