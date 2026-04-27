using AuthService.Domain.Errors;

namespace AuthService.Domain.Exceptions;

/// <summary>
/// Base exception for all domain-related errors
/// Provides structured error information for better handling
/// </summary>
public abstract class DomainException : Exception
{
    /// <summary>
    /// Unique error code for this exception type
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// HTTP status code suggestion (for API responses)
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
/// Exception thrown when user validation fails
/// </summary>
public sealed class UserValidationException : DomainException
{
    public UserValidationException(string message, string errorCode) 
        : base(message, errorCode, 400)
    {
    }
}

/// <summary>
/// Exception thrown when user state transition is invalid
/// </summary>
public sealed class InvalidUserStateException : DomainException
{
    public InvalidUserStateException(string message, string errorCode) 
        : base(message, errorCode, 409) // 409 Conflict
    {
    }
}

/// <summary>
/// Exception thrown when user is not found
/// </summary>
public sealed class UserNotFoundException : DomainException
{
    public UserNotFoundException(string message, string errorCode = UserErrorCodes.UserNotFound)
        : base(message, errorCode, 404)
    {
    }
}

/// <summary>
/// Exception thrown when role validation fails
/// </summary>
public sealed class RoleValidationException : DomainException
{
    public RoleValidationException(string message, string errorCode)
        : base(message, errorCode, 400)
    {
    }
}

/// <summary>
/// Exception thrown when role state transition is invalid
/// </summary>
public sealed class InvalidRoleStateException : DomainException
{
    public InvalidRoleStateException(string message, string errorCode)
        : base(message, errorCode, 409) // 409 Conflict
    {
    }
}

/// <summary>
/// Exception thrown when role is not found
/// </summary>
public sealed class RoleNotFoundException : DomainException
{
    public RoleNotFoundException(string message, string errorCode = RoleErrorCodes.RoleNotFound)
        : base(message, errorCode, 404)
    {
    }
}

public sealed class UserFavouriteValidationException : DomainException
{
    public UserFavouriteValidationException(string message, string errorCode)
        : base(message, errorCode, 400)
    {
    }
}

public sealed class SpotifyItemValidationException : DomainException
{
    public SpotifyItemValidationException(string message, string errorCode)
        : base(message, errorCode, 400)
    {
    }
}

