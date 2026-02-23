namespace DDDworkshop.Dam.Rights.Domain.ValueObjects;

/// <summary>
/// The purpose for which an asset is being used.
/// This drives key restriction rules (e.g., "editorial only" assets deny Commercial).
/// </summary>
public enum UsagePurpose
{
    Editorial,
    Commercial,
    Internal,
    Political
}
