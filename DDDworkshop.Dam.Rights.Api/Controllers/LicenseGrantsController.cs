namespace DDDworkshop.Dam.Rights.Api.Controllers;

using DDDworkshop.Dam.Rights.Api.Mapping;
using DDDworkshop.Dam.Rights.Api.Models.Requests;
using DDDworkshop.Dam.Rights.Api.Models.Responses;
using DDDworkshop.Dam.Rights.Application.Commands;
using DDDworkshop.Dam.Rights.Application.Handlers;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Manages license grants: revocation and queries.
/// 
/// POST /license-grants/{grantId}/revoke → Revoke a grant.
/// GET  /license-grants/{grantId}        → Get grant details.
/// GET  /assets/{assetId}/license-grants  → List grants for an asset.
/// </summary>
[ApiController]
[Produces("application/json")]
public sealed class LicenseGrantsController : ControllerBase
{
    private readonly RevokeLicenseHandler _revokeHandler;
    private readonly QueryHandlers _queryHandlers;

    public LicenseGrantsController(
        RevokeLicenseHandler revokeHandler,
        QueryHandlers queryHandlers)
    {
        _revokeHandler = revokeHandler;
        _queryHandlers = queryHandlers;
    }

    /// <summary>
    /// Revoke a license grant.
    /// </summary>
    [HttpPost("license-grants/{grantId:guid}/revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Revoke(
        [FromRoute] Guid grantId,
        [FromBody] RevokeLicenseRequest request,
        CancellationToken ct)
    {
        var command = new RevokeLicenseCommand(
            GrantId: grantId,
            Reason: request.Reason,
            RevokedBy: request.RevokedBy);

        try
        {
            await _revokeHandler.HandleAsync(command, ct);
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
    [ProducesResponseType(typeof(LicenseGrantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LicenseGrantResponse>> GetGrant(
        [FromRoute] Guid grantId,
        CancellationToken ct)
    {
        var dto = await _queryHandlers.GetGrantAsync(grantId, ct);
        if (dto is null)
            return NotFound(new { error = $"License grant '{grantId}' not found." });

        return Ok(ResponseMapper.ToResponse(dto));
    }

    /// <summary>
    /// List license grants for an asset, optionally filtering to active-only.
    /// </summary>
    [HttpGet("assets/{assetId:guid}/license-grants")]
    [ProducesResponseType(typeof(IReadOnlyList<LicenseGrantResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LicenseGrantResponse>>> GetGrantsForAsset(
        [FromRoute] Guid assetId,
        [FromQuery] bool activeOnly = false,
        CancellationToken ct = default)
    {
        var dtos = await _queryHandlers.GetGrantsForAssetAsync(assetId, activeOnly, ct);
        var responses = dtos.Select(ResponseMapper.ToResponse).ToList();
        return Ok(responses);
    }
}
