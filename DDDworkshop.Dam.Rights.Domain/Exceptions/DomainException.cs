namespace DDDworkshop.Dam.Rights.Domain.Exceptions;

/// <summary>
/// Base class for domain exceptions.
/// Domain exceptions represent violations of business rules and invariants.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
    protected DomainException(string message, Exception innerException) : base(message, innerException) { }
}
