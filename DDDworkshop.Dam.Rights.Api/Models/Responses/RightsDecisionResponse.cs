namespace DDDworkshop.Dam.Rights.Api.Models.Responses;

/// <summary>
/// API response for a license request evaluation.
/// </summary>
public sealed class RightsDecisionResponse
{
    public bool IsAllowed { get; init; }
    public Guid? GrantId { get; init; }
    public IReadOnlyList<string> DenialReasons { get; init; } = [];
}
