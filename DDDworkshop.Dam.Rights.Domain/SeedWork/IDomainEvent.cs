namespace DDDworkshop.Dam.Rights.Domain.SeedWork;

/// <summary>
/// Marker interface for domain events.
/// Domain events signal that something meaningful happened in the domain.
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
