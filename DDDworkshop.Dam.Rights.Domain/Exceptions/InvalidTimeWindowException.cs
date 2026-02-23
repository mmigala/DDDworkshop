namespace DDDworkshop.Dam.Rights.Domain.Exceptions;

/// <summary>
/// Thrown when a TimeWindow is constructed with invalid boundaries (start >= end).
/// </summary>
public sealed class InvalidTimeWindowException : DomainException
{
    public DateTimeOffset Start { get; }
    public DateTimeOffset End { get; }

    public InvalidTimeWindowException(DateTimeOffset start, DateTimeOffset end)
        : base($"Invalid time window: start ({start:yyyy-MM-dd}) must be before end ({end:yyyy-MM-dd}).")
    {
        Start = start;
        End = end;
    }
}
