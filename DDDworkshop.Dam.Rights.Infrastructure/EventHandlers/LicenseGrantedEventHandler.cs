namespace DDDworkshop.Dam.Rights.Infrastructure.EventHandlers;

using DDDworkshop.Dam.Rights.Domain.Events;
using DDDworkshop.Dam.Rights.Infrastructure.Services;
using Microsoft.Extensions.Logging;

/// <summary>
/// Sample handler for LicenseGrantedEvent.
/// In a real system this might update a search index, notify downstream systems,
/// create a watermarking job, or log for compliance.
/// 
/// Demonstrates: domain events decouple the "what happened" (domain)
/// from the "what should we do about it" (infrastructure/integration).
/// </summary>
public sealed class LicenseGrantedEventHandler : IDomainEventHandler<LicenseGrantedEvent>
{
    private readonly ILogger<LicenseGrantedEventHandler> _logger;

    public LicenseGrantedEventHandler(ILogger<LicenseGrantedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(LicenseGrantedEvent domainEvent, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[Event] License granted: GrantId={GrantId}, AssetId={AssetId}, Licensee={LicenseeId}, Terms={Terms}",
            domainEvent.GrantId,
            domainEvent.AssetId,
            domainEvent.LicenseeId,
            domainEvent.Terms);

        // In a real system:
        // - Update search index
        // - Notify downstream system
        // - Create watermarking job
        // - Log for compliance

        return Task.CompletedTask;
    }
}
