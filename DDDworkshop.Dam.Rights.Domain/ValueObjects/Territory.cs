namespace DDDworkshop.Dam.Rights.Domain.ValueObjects;

using SeedWork;

/// <summary>
/// Represents a geographic territory as a set of ISO 3166-1 alpha-2 country codes.
/// Immutable value object – territories are compared by their country code sets.
/// 
/// Examples: { "NO" } for Norway, { "NO", "SE", "DK" } for Scandinavia.
/// </summary>
public sealed class Territory : ValueObject
{
    private readonly SortedSet<string> _countryCodes;

    /// <summary>
    /// The set of ISO country codes that make up this territory.
    /// </summary>
    public IReadOnlyCollection<string> CountryCodes => _countryCodes;

    public Territory(IEnumerable<string> countryCodes)
    {
        ArgumentNullException.ThrowIfNull(countryCodes);

        var codes = countryCodes
            .Select(c => c?.Trim().ToUpperInvariant() ?? throw new ArgumentException("Country code cannot be null."))
            .ToList();

        if (codes.Count == 0)
            throw new ArgumentException("Territory must contain at least one country code.");

        if (codes.Any(c => c.Length != 2))
            throw new ArgumentException("Country codes must be ISO 3166-1 alpha-2 (2 characters).");

        _countryCodes = new SortedSet<string>(codes, StringComparer.Ordinal);
    }

    /// <summary>
    /// Returns true if this territory overlaps with another (shares at least one country code).
    /// </summary>
    public bool OverlapsWith(Territory other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return _countryCodes.Overlaps(other._countryCodes);
    }

    /// <summary>
    /// Returns true if this territory fully contains the other territory.
    /// </summary>
    public bool Contains(Territory other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return _countryCodes.IsSupersetOf(other._countryCodes);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        foreach (var code in _countryCodes)
            yield return code;
    }

    public override string ToString() => string.Join(", ", _countryCodes);
}
