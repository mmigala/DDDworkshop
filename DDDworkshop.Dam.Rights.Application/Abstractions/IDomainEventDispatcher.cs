namespace DDDworkshop.Dam.Rights.Application.Abstractions;

using DDDworkshop.Dam.Rights.Domain.SeedWork;

/// <summary>
/// Dispatches domain events collected from aggregates.
/// Called after persistence to ensure events are only published when state is committed.
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default);
}
