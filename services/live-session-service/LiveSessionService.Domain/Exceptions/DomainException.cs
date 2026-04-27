using LiveSessionService.Domain.Errors;

namespace LiveSessionService.Domain.Exceptions;

/// <summary>
/// Base exception for all domain-related errors
/// Provides structured error information for API responses
/// </summary>
public abstract class DomainException : Exception
{
    /// <summary>
    /// Unique error code for this exception type
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Suggested HTTP status code for API responses
    /// </summary>
    public int StatusCode { get; }

    protected DomainException(string message, string errorCode, int statusCode = 400)
        : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }
}

/// <summary>
/// Exception thrown when LiveSession validation fails
/// HTTP 400 Bad Request
/// </summary>
public sealed class LiveSessionValidationException : DomainException
{
    public LiveSessionValidationException(string message, string errorCode)
        : base(message, errorCode, 400)
    {
    }
}

/// <summary>
/// Exception thrown when LiveSession state transition is invalid
/// HTTP 409 Conflict
/// </summary>
public sealed class InvalidSessionStateException : DomainException
{
    public InvalidSessionStateException(string message, string errorCode)
        : base(message, errorCode, 409)
    {
    }
}

/// <summary>
/// Exception thrown when LiveSession is not found
/// HTTP 404 Not Found
/// </summary>
public sealed class LiveSessionNotFoundException : DomainException
{
    public LiveSessionNotFoundException(string message, string errorCode = LiveSessionErrorCodes.SessionNotFound)
        : base(message, errorCode, 404)
    {
    }
}

/// <summary>
/// Exception thrown when user is not authorized for session operation
/// HTTP 403 Forbidden
/// </summary>
public sealed class SessionAuthorizationException : DomainException
{
    public SessionAuthorizationException(string message, string errorCode)
        : base(message, errorCode, 403)
    {
    }
}

/// <summary>
/// Exception thrown when AzuraCast station validation fails
/// HTTP 400 Bad Request
/// </summary>
public sealed class AzuraCastStationValidationException : DomainException
{
    public AzuraCastStationValidationException(string message, string errorCode)
        : base(message, errorCode, 400)
    {
    }
}

/// <summary>
/// Exception thrown when AzuraCast station state transition is invalid
/// HTTP 409 Conflict
/// </summary>
public sealed class InvalidStationStateException : DomainException
{
    public InvalidStationStateException(string message, string errorCode)
        : base(message, errorCode, 409)
    {
    }
}

/// <summary>
/// Exception thrown when AzuraCast station is not found
/// HTTP 404 Not Found
/// </summary>
public sealed class AzuraCastStationNotFoundException : DomainException
{
    public AzuraCastStationNotFoundException(string message, string errorCode = AzuraCastStationErrorCodes.StationNotFound)
        : base(message, errorCode, 404)
    {
    }
}

/// <summary>
/// Exception thrown when NowPlaying validation fails
/// HTTP 400 Bad Request
/// </summary>
public sealed class NowPlayingValidationException : DomainException
{
    public NowPlayingValidationException(string message, string errorCode)
        : base(message, errorCode, 400)
    {
    }
}

/// <summary>
/// Exception thrown when NowPlaying sync operation fails
/// HTTP 500 Internal Server Error
/// </summary>
public sealed class NowPlayingSyncException : DomainException
{
    public NowPlayingSyncException(string message, string errorCode)
        : base(message, errorCode, 500)
    {
    }
}
