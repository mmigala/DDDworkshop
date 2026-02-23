namespace DDDworkshop.Dam.Rights.Domain.SeedWork;

/// <summary>
/// Base class for aggregate roots.
/// An aggregate root is the entry point to a cluster of domain objects (the aggregate).
/// It enforces all invariants for the aggregate and collects domain events.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Domain events raised by this aggregate, to be dispatched after persistence.
    /// </summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected AggregateRoot(TId id) : base(id) { }

    // EF / serialization
    protected AggregateRoot() { }

    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
