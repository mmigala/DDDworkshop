namespace DDDworkshop.Dam.Rights.Domain.ValueObjects;

using DDDworkshop.Dam.Rights.Domain.Exceptions;
using SeedWork;

/// <summary>
/// Represents a time period with a start and end date.
/// Enforces the invariant that start must be before end.
/// Immutable value object.
/// </summary>
public sealed class TimeWindow : ValueObject
{
    public DateTimeOffset Start { get; }
    public DateTimeOffset End { get; }

    public TimeWindow(DateTimeOffset start, DateTimeOffset end)
    {
        if (start >= end)
            throw new InvalidTimeWindowException(start, end);

        Start = start;
        End = end;
    }

    /// <summary>
    /// Returns the duration of this time window.
    /// </summary>
    public TimeSpan Duration => End - Start;

    /// <summary>
    /// Returns true if this time window overlaps with another.
    /// Two windows overlap if one starts before the other ends, and vice versa.
    /// </summary>
    public bool OverlapsWith(TimeWindow other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Start < other.End && other.Start < End;
    }

    /// <summary>
    /// Returns true if the given point in time falls within this window.
    /// </summary>
    public bool Contains(DateTimeOffset pointInTime)
    {
        return pointInTime >= Start && pointInTime < End;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Start;
        yield return End;
    }

    public override string ToString() => $"{Start:yyyy-MM-dd} → {End:yyyy-MM-dd}";
}
