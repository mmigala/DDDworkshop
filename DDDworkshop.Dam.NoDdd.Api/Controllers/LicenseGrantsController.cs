namespace DDDworkshop.Dam.NoDdd.Api.Controllers;

using DDDworkshop.Dam.NoDdd.Api.Data;
using DDDworkshop.Dam.NoDdd.Api.Services;
using Microsoft.AspNetCore.Mvc;

// ⚠️ ANTI-PATTERN: Controller directly exposes raw entity data.
// No DTO mapping layer — renaming an entity property breaks the API.

[ApiController]
[Produces("application/json")]
public class LicenseGrantsController : ControllerBase
{
    private readonly LicenseService _licenseService;
    private readonly InMemoryDataStore _store;

    public LicenseGrantsController(LicenseService licenseService, InMemoryDataStore store)
    {
        _licenseService = licenseService;
        _store = store;
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

    // ──────────────────────────────────────────────
    // ⚠️ ANTI-PATTERN: A "quick" admin endpoint written by a different developer.
    // They didn't use LicenseService.RevokeLicense() — they went straight to the data store.
    //
    // What's missing:
    //   1. No "cannot revoke expired grant" guard (service has it, this doesn't)
    //   2. No "already revoked" guard
    //   3. No exclusive window cleanup
    //   4. No domain events (if they existed)
    //   5. RevokedBy/RevokedAt not set
    //
    // This is EXACTLY the scattered validation problem:
    //   - LicenseService.RevokeLicense() has the correct logic
    //   - This endpoint bypasses it entirely
    //   - Both "work" — but this one creates invalid state
    //
    // In DDD, this CAN'T happen: LicenseGrant.Revoke() is the ONLY way to revoke,
    // and it enforces all guards internally. There's no data bag to mutate directly.
    // ──────────────────────────────────────────────

    /// <summary>
    /// Bulk-revoke all grants for an asset (admin shortcut).
    /// </summary>
    [HttpPost("assets/{assetId:guid}/license-grants/bulk-revoke")]
    public IActionResult BulkRevoke(
        [FromRoute] Guid assetId,
        [FromBody] BulkRevokeBody body)
    {
        // ⚠️ Goes directly to the data store — bypasses LicenseService entirely.
        var grants = _store.LicenseGrants.Values
            .Where(g => g.AssetId == assetId && g.Status == "Issued")
            .ToList();

        if (grants.Count == 0)
            return NotFound(new { error = "No active grants found for this asset." });

        foreach (var grant in grants)
        {
            // ⚠️ Just flips the status string — no guards, no cleanup, no events.
            // An expired grant could sneak in if timing is unlucky (TOCTOU).
            // RevokedBy, RevokedAt, RevocationReason are NOT set → incomplete audit trail.
            // Exclusive windows are NOT removed → future exclusive requests will be wrongly denied.
            grant.Status = "Revoked";
        }

        return Ok(new
        {
            message = $"Bulk-revoked {grants.Count} grant(s).",
            revokedGrantIds = grants.Select(g => g.Id)
        });
    }
}

public class RevokeBody
{
    public string Reason { get; set; } = default!;
    public string RevokedBy { get; set; } = default!;
}

public class BulkRevokeBody
{
    public string Reason { get; set; } = default!;
    public string RevokedBy { get; set; } = default!;
}
