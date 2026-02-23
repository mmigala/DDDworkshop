namespace DDDworkshop.Dam.Rights.Domain.ValueObjects;

/// <summary>
/// The channel through which an asset is used.
/// Modeled as an enum – a closed set of known distribution channels.
/// </summary>
public enum UsageChannel
{
    Web,
    Print,
    Social,
    Tv,
    Broadcast,
    InternalUse
}
