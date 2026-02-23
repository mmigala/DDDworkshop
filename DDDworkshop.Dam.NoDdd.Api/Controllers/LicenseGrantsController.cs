namespace DDDworkshop.Dam.NoDdd.Api.Controllers;

using DDDworkshop.Dam.NoDdd.Api.Services;
using Microsoft.AspNetCore.Mvc;

// ⚠️ ANTI-PATTERN: Controller directly exposes raw entity data.
// No DTO mapping layer — renaming an entity property breaks the API.

[ApiController]
[Produces("application/json")]
public class LicenseGrantsController : ControllerBase
{
    private readonly LicenseService _licenseService;

    public LicenseGrantsController(LicenseService licenseService)
    {
        _licenseService = licenseService;
    }

    /// <summary>
    /// Revoke a license grant.
    /// </summary>
    [HttpPost("license-grants/{grantId:guid}/revoke")]
    public IActionResult Revoke(
        [FromRoute] Guid grantId,
        [FromBody] RevokeBody body)
    {
        try
        {
            _licenseService.RevokeLicense(grantId, body.Reason, body.RevokedBy);
            return NoContent();
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

    /// <summary>
    /// Get details of a specific license grant.
    /// </summary>
    [HttpGet("license-grants/{grantId:guid}")]
    public IActionResult GetGrant([FromRoute] Guid grantId)
    {
        var grant = _licenseService.GetGrant(grantId);
        if (grant is null)
            return NotFound(new { error = $"License grant '{grantId}' not found." });

        // ⚠️ Returning raw entity — any entity property change breaks the API contract.
        return Ok(new
        {
            grant.Id,
            grant.AssetId,
            grant.LicenseeId,
            grant.Channel,
            territoryCodes = grant.Territory.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            timeWindowStart = grant.Start,
            timeWindowEnd = grant.End,
            grant.Purpose,
            grant.IsExclusive,
            grant.Status,
            grant.IssuedAt,
            grant.ExpiresAt,
            grant.RevocationReason,
            grant.RevokedBy,
            grant.RevokedAt
        });
    }

    /// <summary>
    /// List license grants for an asset.
    /// </summary>
    [HttpGet("assets/{assetId:guid}/license-grants")]
    public IActionResult GetGrantsForAsset(
        [FromRoute] Guid assetId,
        [FromQuery] bool activeOnly = false)
    {
        var grants = _licenseService.GetGrantsForAsset(assetId, activeOnly);

        // ⚠️ Inline mapping from raw entity to response shape — no reusable mapper.
        var result = grants.Select(g => new
        {
            g.Id,
            g.AssetId,
            g.LicenseeId,
            g.Channel,
            territoryCodes = g.Territory.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            timeWindowStart = g.Start,
            timeWindowEnd = g.End,
            g.Purpose,
            g.IsExclusive,
            g.Status,
            g.IssuedAt,
            g.ExpiresAt,
            g.RevocationReason,
            g.RevokedBy,
            g.RevokedAt
        });

        return Ok(result);
    }
}

public class RevokeBody
{
    public string Reason { get; set; } = default!;
    public string RevokedBy { get; set; } = default!;
}
