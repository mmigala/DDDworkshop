namespace DDDworkshop.Dam.Rights.Api.Controllers;

using DDDworkshop.Dam.Rights.Api.Mapping;
using DDDworkshop.Dam.Rights.Api.Models.Requests;
using DDDworkshop.Dam.Rights.Api.Models.Responses;
using DDDworkshop.Dam.Rights.Application.Commands;
using DDDworkshop.Dam.Rights.Application.Handlers;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Manages an asset's rights profile: owner, release status, restrictions, exclusive windows.
/// 
/// PUT  /assets/{assetId}/rights-profile                   → Create or update profile.
/// GET  /assets/{assetId}/rights-profile                   → Get profile.
/// POST /assets/{assetId}/rights-profile/restrictions      → Add a restriction.
/// POST /assets/{assetId}/rights-profile/exclusive-windows → Add an exclusive window.
/// </summary>
[ApiController]
[Route("assets/{assetId:guid}/rights-profile")]
[Produces("application/json")]
public sealed class RightsProfileController : ControllerBase
{
    private readonly SetRightsProfileHandler _setProfileHandler;
    private readonly AddRestrictionHandler _addRestrictionHandler;
    private readonly AddExclusiveWindowHandler _addExclusiveWindowHandler;
    private readonly QueryHandlers _queryHandlers;

    public RightsProfileController(
        SetRightsProfileHandler setProfileHandler,
        AddRestrictionHandler addRestrictionHandler,
        AddExclusiveWindowHandler addExclusiveWindowHandler,
        QueryHandlers queryHandlers)
    {
        _setProfileHandler = setProfileHandler;
        _addRestrictionHandler = addRestrictionHandler;
        _addExclusiveWindowHandler = addExclusiveWindowHandler;
        _queryHandlers = queryHandlers;
    }

    /// <summary>
    /// Create or update an asset's rights profile.
    /// </summary>
    [HttpPut]
    [ProducesResponseType(typeof(AssetRightsProfileResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AssetRightsProfileResponse>> SetProfile(
        [FromRoute] Guid assetId,
        [FromBody] SetRightsProfileRequest request,
        CancellationToken ct)
    {
        var command = new SetRightsProfileCommand(
            AssetId: assetId,
            OwnerId: request.OwnerId,
            ReleaseStatus: request.ReleaseStatus);

        var dto = await _setProfileHandler.HandleAsync(command, ct);
        return Ok(ResponseMapper.ToResponse(dto));
    }

    /// <summary>
    /// Get an asset's rights profile.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(AssetRightsProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetRightsProfileResponse>> GetProfile(
        [FromRoute] Guid assetId,
        CancellationToken ct)
    {
        var dto = await _queryHandlers.GetRightsProfileAsync(assetId, ct);
        if (dto is null)
            return NotFound(new { error = $"Rights profile not found for asset '{assetId}'." });

        return Ok(ResponseMapper.ToResponse(dto));
    }

    /// <summary>
    /// Add a restriction to an asset's rights profile.
    /// </summary>
    [HttpPost("restrictions")]
    [ProducesResponseType(typeof(RestrictionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RestrictionResponse>> AddRestriction(
        [FromRoute] Guid assetId,
        [FromBody] AddRestrictionRequest request,
        CancellationToken ct)
    {
        var command = new AddRestrictionCommand(
            AssetId: assetId,
            Description: request.Description,
            RestrictedChannel: request.RestrictedChannel,
            RestrictedPurpose: request.RestrictedPurpose,
            RestrictedTerritoryCodes: request.RestrictedTerritoryCodes,
            RequiresRelease: request.RequiresRelease);

        try
        {
            var dto = await _addRestrictionHandler.HandleAsync(command, ct);
            var response = ResponseMapper.ToResponse(dto);
            return CreatedAtAction(nameof(GetProfile), new { assetId }, response);
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
    [ProducesResponseType(typeof(ExclusiveWindowResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ExclusiveWindowResponse>> AddExclusiveWindow(
        [FromRoute] Guid assetId,
        [FromBody] AddExclusiveWindowRequest request,
        CancellationToken ct)
    {
        var command = new AddExclusiveWindowCommand(
            AssetId: assetId,
            GrantId: request.GrantId,
            Channel: request.Channel,
            TerritoryCodes: request.TerritoryCodes,
            TimeWindowStart: request.TimeWindowStart,
            TimeWindowEnd: request.TimeWindowEnd,
            Purpose: request.Purpose);

        try
        {
            var dto = await _addExclusiveWindowHandler.HandleAsync(command, ct);
            var response = ResponseMapper.ToResponse(dto);
            return CreatedAtAction(nameof(GetProfile), new { assetId }, response);
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
}
