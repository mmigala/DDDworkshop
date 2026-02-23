namespace DDDworkshop.Dam.Rights.Application.Dtos;

/// <summary>
/// DTO representing the result of a license request evaluation.
/// </summary>
public sealed record RightsDecisionDto(
    bool IsAllowed,
    Guid? GrantId,
    IReadOnlyList<string> DenialReasons);
