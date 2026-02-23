namespace DDDworkshop.Dam.Rights.Api.Models.Requests;

/// <summary>
/// API request model for revoking a license grant.
/// </summary>
public sealed class RevokeLicenseRequest
{
    public string Reason { get; init; } = default!;
    public string RevokedBy { get; init; } = default!;
}
