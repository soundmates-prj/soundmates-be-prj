namespace AuthService.Domain.Errors;

/// <summary>
/// Error codes for password validation
/// Centralized constants for password business rules
/// </summary>
public static class PasswordErrorCodes
{
    public const string PasswordEmpty = "PASSWORD_EMPTY";
    public const string PasswordTooShort = "PASSWORD_TOO_SHORT";
    public const string PasswordMissingLowercase = "PASSWORD_MISSING_LOWERCASE";
    public const string PasswordMissingUppercase = "PASSWORD_MISSING_UPPERCASE";
    public const string PasswordMissingNumber = "PASSWORD_MISSING_NUMBER";
    public const string PasswordMissingSpecialChar = "PASSWORD_MISSING_SPECIAL_CHAR";
}
