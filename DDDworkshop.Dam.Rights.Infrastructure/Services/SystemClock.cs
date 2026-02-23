namespace DDDworkshop.Dam.Rights.Infrastructure.Services;

using DDDworkshop.Dam.Rights.Application.Abstractions;

/// <summary>
/// Simple system clock implementation.
/// In production this just returns the current time.
/// In tests, you can substitute a fake clock to control time precisely.
/// </summary>
public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
