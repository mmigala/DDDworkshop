namespace DDDworkshop.Dam.Rights.Api.Models.Requests;

/// <summary>
/// API request model for requesting a license.
/// Separate from the application command – controllers map request → command.
/// This keeps the API contract independent of internal command shape.
/// </summary>
public sealed class RequestLicenseRequest
{
    public Guid LicenseeId { get; init; }
    public string Channel { get; init; } = default!;
    public List<string> TerritoryCodes { get; init; } = [];
    public DateTimeOffset TimeWindowStart { get; init; }
    public DateTimeOffset TimeWindowEnd { get; init; }
    public string Purpose { get; init; } = default!;
    public bool IsExclusive { get; init; }
}
