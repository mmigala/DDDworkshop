namespace DDDworkshop.Dam.Rights.Api.Controllers;

using DDDworkshop.Dam.Rights.Api.Mapping;
using DDDworkshop.Dam.Rights.Api.Models.Requests;
using DDDworkshop.Dam.Rights.Api.Models.Responses;
using DDDworkshop.Dam.Rights.Application.Commands;
using DDDworkshop.Dam.Rights.Application.Handlers;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Handles license request evaluation.
/// 
/// POST /assets/{assetId}/license-requests
///   → Evaluates rights, checks exclusivity, issues grant if allowed.
/// 
/// The controller is thin: it maps the HTTP request to an application command,
/// delegates to the handler, and maps the result back to an API response.
/// No business logic here.
/// </summary>
[ApiController]
[Route("assets/{assetId:guid}/license-requests")]
[Produces("application/json")]
public sealed class LicenseRequestsController : ControllerBase
{
    private readonly RequestLicenseHandler _handler;

    public LicenseRequestsController(RequestLicenseHandler handler)
    {
        _handler = handler;
    }

    /// <summary>
    /// Request a license for an asset.
    /// </summary>
    /// <param name="assetId">The asset to license.</param>
    /// <param name="request">License request details.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Rights decision indicating whether the license was granted or denied.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(RightsDecisionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RightsDecisionResponse>> RequestLicense(
        [FromRoute] Guid assetId,
        [FromBody] RequestLicenseRequest request,
        CancellationToken ct)
    {
        var command = new RequestLicenseCommand(
            AssetId: assetId,
            LicenseeId: request.LicenseeId,
            Channel: request.Channel,
            TerritoryCodes: request.TerritoryCodes,
            TimeWindowStart: request.TimeWindowStart,
            TimeWindowEnd: request.TimeWindowEnd,
            Purpose: request.Purpose,
            IsExclusive: request.IsExclusive);

        try
        {
            var result = await _handler.HandleAsync(command, ct);
            return Ok(ResponseMapper.ToResponse(result));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new { error = ex.Message });
        }
    }
}
