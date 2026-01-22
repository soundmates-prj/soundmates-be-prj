namespace AuthService.Domain.Interfaces;

/// <summary>
/// Abstraction for DateTime operations to avoid direct dependency on System.DateTime
/// This allows for testability and follows Clean Architecture principles
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>
    /// Gets the current UTC date and time
    /// </summary>
    DateTime UtcNow { get; }
}
