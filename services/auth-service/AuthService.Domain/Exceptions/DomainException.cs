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
/// Error codes for user-related operations
/// Centralized for consistency and easy maintenance
/// </summary>
public static class UserErrorCodes
{
    // Validation errors (400)
    public const string UsernameEmpty = "USER_USERNAME_EMPTY";
    public const string EmailEmpty = "USER_EMAIL_EMPTY";
    public const string EmailInvalid = "USER_EMAIL_INVALID";
    public const string PasswordEmpty = "USER_PASSWORD_EMPTY";
    public const string FirstNameEmpty = "USER_FIRSTNAME_EMPTY";
    public const string LastNameEmpty = "USER_LASTNAME_EMPTY";
    public const string TokenEmpty = "USER_TOKEN_EMPTY";

    // State transition errors (409 Conflict)
    public const string AlreadyActive = "USER_ALREADY_ACTIVE";
    public const string AlreadyInactive = "USER_ALREADY_INACTIVE";
    public const string CannotBanInactive = "USER_CANNOT_BAN_INACTIVE";
    public const string EmailAlreadyVerified = "USER_EMAIL_ALREADY_VERIFIED";
}
