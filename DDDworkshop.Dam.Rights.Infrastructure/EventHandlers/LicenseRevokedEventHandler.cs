namespace DDDworkshop.Dam.Rights.Infrastructure.EventHandlers;

using DDDworkshop.Dam.Rights.Domain.Events;
using DDDworkshop.Dam.Rights.Infrastructure.Services;
using Microsoft.Extensions.Logging;

/// <summary>
/// Sample handler for LicenseRevokedEvent.
/// </summary>
public sealed class LicenseRevokedEventHandler : IDomainEventHandler<LicenseRevokedEvent>
{
    private readonly ILogger<LicenseRevokedEventHandler> _logger;

    public LicenseRevokedEventHandler(ILogger<LicenseRevokedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(LicenseRevokedEvent domainEvent, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[Event] License revoked: GrantId={GrantId}, AssetId={AssetId}, Reason={Reason}, RevokedBy={RevokedBy}",
            domainEvent.GrantId,
            domainEvent.AssetId,
            domainEvent.Reason,
            domainEvent.RevokedBy);

        // In a real system:
        // - Remove watermark authorization
        // - Notify licensee
        // - Log for compliance audit

        return Task.CompletedTask;
    }
}
