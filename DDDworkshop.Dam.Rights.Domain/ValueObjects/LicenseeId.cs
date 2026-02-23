namespace DDDworkshop.Dam.Rights.Domain.ValueObjects;

using SeedWork;

/// <summary>
/// Strongly-typed identifier for a Licensee (customer requesting a license).
/// </summary>
public sealed class LicenseeId : ValueObject
{
    public Guid Value { get; }

    public LicenseeId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("LicenseeId cannot be empty.", nameof(value));

        Value = value;
    }

    public static LicenseeId New() => new(Guid.NewGuid());

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
