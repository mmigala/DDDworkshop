namespace DDDworkshop.Dam.NoDdd.Api.Controllers;

using DDDworkshop.Dam.NoDdd.Api.Services;
using Microsoft.AspNetCore.Mvc;

// ⚠️ ANTI-PATTERN: Controller calls service directly, and service returns raw entities.
// No separation of concerns — the HTTP layer is tightly coupled to the "domain" (entities).
// Renaming an entity property breaks the API contract.

[ApiController]
[Route("assets/{assetId:guid}/license-requests")]
[Produces("application/json")]
public class LicenseRequestsController : ControllerBase
{
    private readonly LicenseService _licenseService;

    public LicenseRequestsController(LicenseService licenseService)
    {
        _licenseService = licenseService;
    }

    /// <summary>
    /// Request a license for an asset.
    /// </summary>
    [HttpPost]
    public IActionResult RequestLicense(
        [FromRoute] Guid assetId,
        [FromBody] LicenseRequestBody body)
    {
        try
        {
            // ⚠️ Territory converted to comma-separated string — lossy, fragile.
            var territory = string.Join(",", body.TerritoryCodes);

            var (isAllowed, grantId, reasons) = _licenseService.IssueLicense(
                assetId,
                body.LicenseeId,
                body.Channel,
                territory,
                body.TimeWindowStart,
                body.TimeWindowEnd,
                body.Purpose,
                body.IsExclusive);

            return Ok(new
            {
                isAllowed,
                grantId,
                denialReasons = reasons
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new { error = ex.Message });
        }
    }
}

// ⚠️ Request body class lives alongside the controller — no clear layering.
public class LicenseRequestBody
{
    public Guid LicenseeId { get; set; }
    public string Channel { get; set; } = default!;
    public List<string> TerritoryCodes { get; set; } = [];
    public DateTimeOffset TimeWindowStart { get; set; }
    public DateTimeOffset TimeWindowEnd { get; set; }
    public string Purpose { get; set; } = default!;
    public bool IsExclusive { get; set; }
}
