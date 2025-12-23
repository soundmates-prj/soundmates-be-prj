using System.Text.RegularExpressions;

namespace AuthService.Application.Services.Common
{
    public static class PasswordValidator
    {
        public static (bool IsValid, string? ErrorMessage) Validate(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                return (false, "Password cannot be empty");
            }

            if (password.Length < 8)
            {
                return (false, "Password must be at least 8 characters long");
            }

            if (!Regex.IsMatch(password, @"[a-z]"))
            {
                return (false, "Password must contain at least one lowercase letter");
            }

            if (!Regex.IsMatch(password, @"[A-Z]"))
            {
                return (false, "Password must contain at least one uppercase letter");
            }

            if (!Regex.IsMatch(password, @"[0-9]"))
            {
                return (false, "Password must contain at least one number");
            }

            if (!Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]"))
            {
                return (false, "Password must contain at least one special character");
            }

            return (true, null);
        }
    }
}

