using System.Text.RegularExpressions;
using AuthService.Domain.Errors;
using AuthService.Domain.Exceptions;

namespace AuthService.Domain.Rules;

/// <summary>
/// Domain Rule for password validation
/// Encapsulates password validation business rules in the Domain layer
/// </summary>
public static class PasswordRule
{
    private const int MinLength = 8;

    /// <summary>
    /// Validates password according to business rules
    /// Throws UserValidationException if password is invalid
    /// </summary>
    /// <param name="password">Password to validate</param>
    /// <exception cref="UserValidationException">When password doesn't meet requirements</exception>
    public static void Validate(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new UserValidationException(
                "Password cannot be empty",
                PasswordErrorCodes.PasswordEmpty);
        }

        if (password.Length < MinLength)
        {
            throw new UserValidationException(
                $"Password must be at least {MinLength} characters long",
                PasswordErrorCodes.PasswordTooShort);
        }

        if (!Regex.IsMatch(password, @"[a-z]"))
        {
            throw new UserValidationException(
                "Password must contain at least one lowercase letter",
                PasswordErrorCodes.PasswordMissingLowercase);
        }

        if (!Regex.IsMatch(password, @"[A-Z]"))
        {
            throw new UserValidationException(
                "Password must contain at least one uppercase letter",
                PasswordErrorCodes.PasswordMissingUppercase);
        }

        if (!Regex.IsMatch(password, @"[0-9]"))
        {
            throw new UserValidationException(
                "Password must contain at least one number",
                PasswordErrorCodes.PasswordMissingNumber);
        }

        if (!Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]"))
        {
            throw new UserValidationException(
                "Password must contain at least one special character",
                PasswordErrorCodes.PasswordMissingSpecialChar);
        }
    }

    /// <summary>
    /// Checks if password meets all requirements without throwing exception
    /// </summary>
    /// <returns>True if password is valid, false otherwise</returns>
    public static bool IsValid(string password)
    {
        try
        {
            Validate(password);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

