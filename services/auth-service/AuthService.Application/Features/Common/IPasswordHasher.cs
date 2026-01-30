namespace AuthService.Application.Features.Common;

/// <summary>
/// Application service interface for password hashing
/// This is an APPLICATION CONCERN, not domain concern
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hash a plain text password
    /// </summary>
    string HashPassword(string plainPassword);
    
    /// <summary>
    /// Verify a plain text password against a hash
    /// </summary>
    bool VerifyPassword(string plainPassword, string hashedPassword);
}
