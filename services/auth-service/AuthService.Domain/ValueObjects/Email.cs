using System.Text.RegularExpressions;
using AuthService.Domain.Exceptions;

namespace AuthService.Domain.ValueObjects;

/// <summary>
/// Value Object representing a valid email address
/// Ensures email is always in valid format (immutable)
/// </summary>
public sealed record Email
{
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new Email value object with validation
    /// </summary>
    /// <param name="email">Email string to validate</param>
    /// <returns>Valid Email object</returns>
    /// <exception cref="UserValidationException">When email is invalid</exception>
    public static Email Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new UserValidationException(
                "Email cannot be empty", 
                UserErrorCodes.EmailEmpty);

        var normalizedEmail = email.Trim().ToLowerInvariant();

        if (!EmailRegex.IsMatch(normalizedEmail))
            throw new UserValidationException(
                "Invalid email format", 
                UserErrorCodes.EmailInvalid);

        return new Email(normalizedEmail);
    }

    /// <summary>
    /// Implicit conversion from Email to string for convenience
    /// </summary>
    public static implicit operator string(Email email) => email.Value;

    public override string ToString() => Value;
}
