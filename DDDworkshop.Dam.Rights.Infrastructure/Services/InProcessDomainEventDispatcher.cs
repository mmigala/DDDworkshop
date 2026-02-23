namespace DDDworkshop.Dam.Rights.Infrastructure.Services;

using DDDworkshop.Dam.Rights.Application.Abstractions;
using DDDworkshop.Dam.Rights.Domain.SeedWork;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// In-process domain event dispatcher.
/// 
/// After an aggregate is saved, the application layer calls DispatchAsync
/// with the collected domain events. The dispatcher resolves all registered
/// handlers from DI and invokes them synchronously (in-process).
/// 
/// This keeps things simple for the workshop. In production you might
/// use an outbox pattern or a message bus for reliability.
/// </summary>
public sealed class InProcessDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InProcessDomainEventDispatcher> _logger;

    public InProcessDomainEventDispatcher(IServiceProvider serviceProvider, ILogger<InProcessDomainEventDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default)
    {
        foreach (var domainEvent in events)
        {
            _logger.LogInformation("Dispatching domain event: {EventType}", domainEvent.GetType().Name);

            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
            var handlers = _serviceProvider.GetServices(handlerType);

            foreach (var handler in handlers)
            {
                var method = handlerType.GetMethod("HandleAsync");
                if (method is not null)
                {
                    await (Task)method.Invoke(handler, [domainEvent, ct])!;
                }
            }
        }
    }
}
