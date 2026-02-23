namespace DDDworkshop.Dam.Rights.Domain.ValueObjects;

using SeedWork;

/// <summary>
/// Describes the scope of a license: what channel, territory, time period, and purpose it covers.
/// This is the "shape" of a usage request or an exclusive reservation.
/// Immutable value object.
/// </summary>
public sealed class LicenseScope : ValueObject
{
    public UsageChannel Channel { get; }
    public Territory Territory { get; }
    public TimeWindow TimeWindow { get; }
    public UsagePurpose Purpose { get; }

    public LicenseScope(UsageChannel channel, Territory territory, TimeWindow timeWindow, UsagePurpose purpose)
    {
        Channel = channel;
        Territory = territory ?? throw new ArgumentNullException(nameof(territory));
        TimeWindow = timeWindow ?? throw new ArgumentNullException(nameof(timeWindow));
        Purpose = purpose;
    }

    /// <summary>
    /// Returns true if this scope overlaps with another scope.
    /// Overlap requires the same channel AND purpose, overlapping territories, AND overlapping time windows.
    /// </summary>
    public bool OverlapsWith(LicenseScope other)
    {
        ArgumentNullException.ThrowIfNull(other);

        return Channel == other.Channel
            && Purpose == other.Purpose
            && Territory.OverlapsWith(other.Territory)
            && TimeWindow.OverlapsWith(other.TimeWindow);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Channel;
        yield return Territory;
        yield return TimeWindow;
        yield return Purpose;
    }

    public override string ToString()
        => $"{Purpose} / {Channel} / {Territory} / {TimeWindow}";
}
