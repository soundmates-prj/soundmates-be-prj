using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Infrastructure.Services;

/// <summary>
/// Production implementation of IDateTimeProvider
/// Returns current system time
/// </summary>
public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
    
    public DateTime Now => DateTime.Now;
}
