namespace DDDworkshop.Dam.Rights.Application.Abstractions;

/// <summary>
/// Abstraction for the system clock.
/// Using an interface allows tests to control time precisely,
/// and keeps the application layer independent of static DateTime calls.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
