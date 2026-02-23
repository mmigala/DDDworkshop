namespace DDDworkshop.Dam.Rights.Domain.ValueObjects;

using SeedWork;

/// <summary>
/// Strongly-typed identifier for the asset Owner / Licensor.
/// </summary>
public sealed class OwnerId : ValueObject
{
    public Guid Value { get; }

    public OwnerId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("OwnerId cannot be empty.", nameof(value));

        Value = value;
    }

    public static OwnerId New() => new(Guid.NewGuid());

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
