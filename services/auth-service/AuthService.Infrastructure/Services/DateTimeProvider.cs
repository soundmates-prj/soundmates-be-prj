using AuthService.Domain.Interfaces;

namespace AuthService.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of IDateTimeProvider
/// Provides actual system time
/// </summary>
public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
