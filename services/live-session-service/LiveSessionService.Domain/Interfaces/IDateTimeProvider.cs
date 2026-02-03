namespace LiveSessionService.Domain.Interfaces;

/// <summary>
/// Abstraction for date/time operations
/// Enables testing with fixed timestamps
/// </summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
    DateTime Now { get; }
}
