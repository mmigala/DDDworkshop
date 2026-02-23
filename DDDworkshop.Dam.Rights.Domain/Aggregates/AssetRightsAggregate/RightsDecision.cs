namespace DDDworkshop.Dam.Rights.Domain.Aggregates.AssetRightsAggregate;

/// <summary>
/// The result of evaluating a license request against an asset's rights profile.
/// Either Allowed (license can be issued) or Denied (with one or more reasons).
/// 
/// This is a domain result type, not an exception – denied requests are expected outcomes.
/// </summary>
public sealed class RightsDecision
{
    public bool IsAllowed { get; }
    public IReadOnlyList<string> DenialReasons { get; }

    private RightsDecision(bool isAllowed, IReadOnlyList<string> denialReasons)
    {
        IsAllowed = isAllowed;
        DenialReasons = denialReasons;
    }

    public static RightsDecision Allowed() => new(true, []);

    public static RightsDecision Denied(string reason) => new(false, [reason]);

    public static RightsDecision Denied(IEnumerable<string> reasons)
    {
        var list = reasons.ToList().AsReadOnly();
        if (list.Count == 0)
            throw new ArgumentException("At least one denial reason is required.");
        return new(false, list);
    }

    /// <summary>
    /// Combines multiple decisions. If any is denied, the result is denied with all reasons merged.
    /// </summary>
    public static RightsDecision Combine(IEnumerable<RightsDecision> decisions)
    {
        var allReasons = decisions
            .Where(d => !d.IsAllowed)
            .SelectMany(d => d.DenialReasons)
            .ToList();

        return allReasons.Count > 0
            ? Denied(allReasons)
            : Allowed();
    }

    public override string ToString()
        => IsAllowed ? "Allowed" : $"Denied: {string.Join("; ", DenialReasons)}";
}
