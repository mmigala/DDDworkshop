namespace DDDworkshop.Dam.Rights.Domain.ValueObjects;

using SeedWork;

/// <summary>
/// Strongly-typed identifier for an Asset.
/// Wrapping a primitive in a value object prevents mixing up different IDs (e.g. passing a LicenseeId where an AssetId is expected).
/// </summary>
public sealed class AssetId : ValueObject
{
    public Guid Value { get; }

    public AssetId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("AssetId cannot be empty.", nameof(value));

        Value = value;
    }

    public static AssetId New() => new(Guid.NewGuid());

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
