namespace DDDworkshop.Dam.Rights.Infrastructure.Services;

using DDDworkshop.Dam.Rights.Domain.SeedWork;

/// <summary>
/// Interface for handling a specific type of domain event.
/// Registered in DI and resolved by the dispatcher.
/// </summary>
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken ct = default);
}
