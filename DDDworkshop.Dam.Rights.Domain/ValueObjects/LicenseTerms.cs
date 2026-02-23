namespace DDDworkshop.Dam.Rights.Domain.ValueObjects;

using SeedWork;

/// <summary>
/// The terms of a license request or granted license.
/// Combines the scope with an exclusivity flag.
/// Immutable – once a license is issued, its terms cannot change (revoke and re-issue instead).
/// </summary>
public sealed class LicenseTerms : ValueObject
{
    public LicenseScope Scope { get; }
    public bool IsExclusive { get; }

    public LicenseTerms(LicenseScope scope, bool isExclusive)
    {
        Scope = scope ?? throw new ArgumentNullException(nameof(scope));
        IsExclusive = isExclusive;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Scope;
        yield return IsExclusive;
    }

    public override string ToString()
        => $"{Scope}{(IsExclusive ? " [EXCLUSIVE]" : "")}";
}
