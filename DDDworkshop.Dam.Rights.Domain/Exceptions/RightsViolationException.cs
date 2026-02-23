namespace DDDworkshop.Dam.Rights.Domain.Exceptions;

/// <summary>
/// Thrown when a rights evaluation fails – e.g., a license request is denied
/// because it violates a restriction or conflicts with an exclusive window.
/// </summary>
public sealed class RightsViolationException : DomainException
{
    public IReadOnlyList<string> Reasons { get; }

    public RightsViolationException(string reason)
        : base(reason)
    {
        Reasons = [reason];
    }

    public RightsViolationException(IEnumerable<string> reasons)
        : base($"Rights violation: {string.Join("; ", reasons)}")
    {
        Reasons = reasons.ToList().AsReadOnly();
    }
}
