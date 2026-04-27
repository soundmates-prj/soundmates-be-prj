namespace AuthService.Domain.Errors;

/// <summary>
/// Error codes for user-related operations
/// Centralized constants for user validation, state, and authentication errors
/// </summary>
public static class UserErrorCodes
{
    // Validation errors (400 Bad Request)
    public const string UsernameEmpty = "USER_USERNAME_EMPTY";
    public const string EmailEmpty = "USER_EMAIL_EMPTY";
    public const string EmailInvalid = "USER_EMAIL_INVALID";
    public const string PasswordEmpty = "USER_PASSWORD_EMPTY";
    public const string FirstNameEmpty = "USER_FIRSTNAME_EMPTY";
    public const string LastNameEmpty = "USER_LASTNAME_EMPTY";
    public const string TokenEmpty = "USER_TOKEN_EMPTY";
    
    // Not found errors (404 Not Found)
    public const string UserNotFound = "USER_NOT_FOUND";

    // State transition errors (409 Conflict)
    public const string AlreadyActive = "USER_ALREADY_ACTIVE";
    public const string AlreadyInactive = "USER_ALREADY_INACTIVE";
    public const string CannotBanInactive = "USER_CANNOT_BAN_INACTIVE";
    public const string EmailAlreadyVerified = "USER_EMAIL_ALREADY_VERIFIED";
    public const string UserAlreadyExists = "USER_ALREADY_EXISTS";
    public const string InvalidStatusTransition = "USER_INVALID_STATUS_TRANSITION";
    
    // Authentication errors (401 Unauthorized)
    public const string InvalidCredentials = "USER_INVALID_CREDENTIALS";
    public const string EmailNotVerified = "USER_EMAIL_NOT_VERIFIED";
    public const string AccountInactive = "USER_ACCOUNT_INACTIVE";
}
