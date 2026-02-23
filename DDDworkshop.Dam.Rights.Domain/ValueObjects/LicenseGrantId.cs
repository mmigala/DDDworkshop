namespace DDDworkshop.Dam.Rights.Domain.ValueObjects;

using SeedWork;

/// <summary>
/// Strongly-typed identifier for a License Grant.
/// </summary>
public sealed class LicenseGrantId : ValueObject
{
    public Guid Value { get; }

    public LicenseGrantId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("LicenseGrantId cannot be empty.", nameof(value));

        Value = value;
    }

    public static LicenseGrantId New() => new(Guid.NewGuid());

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
