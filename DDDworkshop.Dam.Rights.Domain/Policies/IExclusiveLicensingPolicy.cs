namespace DDDworkshop.Dam.Rights.Domain.Policies;

using DDDworkshop.Dam.Rights.Domain.ValueObjects;

/// <summary>
/// Domain service interface for checking exclusive licensing conflicts.
/// 
/// This is a great example of why domain services exist:
///   - Exclusivity checks require looking at existing grants (cross-aggregate query).
///   - An aggregate should not query the database itself (isolation).
///   - The policy encapsulates business logic that spans multiple aggregates.
/// 
/// The interface is defined in the Domain layer; implementation lives in the Domain layer too
/// (it's pure logic), but it depends on repository interfaces for data access.
/// </summary>
public interface IExclusiveLicensingPolicy
{
    /// <summary>
    /// Checks whether the requested exclusive scope conflicts with any
    /// existing active grants for the same asset.
    /// </summary>
    /// <returns>
    /// Null if no conflict; otherwise, a description of the conflict.
    /// </returns>
    Task<ExclusivityCheckResult> CheckAsync(AssetId assetId, LicenseScope scope, DateTimeOffset now, CancellationToken ct = default);
}

/// <summary>
/// Result of an exclusivity check.
/// </summary>
public sealed class ExclusivityCheckResult
{
    public bool HasConflict { get; }
    public string? ConflictDescription { get; }

    private ExclusivityCheckResult(bool hasConflict, string? conflictDescription)
    {
        HasConflict = hasConflict;
        ConflictDescription = conflictDescription;
    }

    public static ExclusivityCheckResult NoConflict() => new(false, null);

    public static ExclusivityCheckResult Conflict(string description) => new(true, description);
}
