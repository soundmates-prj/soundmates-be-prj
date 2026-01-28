namespace AuthService.Domain.Enums;

/// <summary>
/// OAuth provider types
/// Represents external authentication providers
/// </summary>
public enum OAuthProvider
{
    /// <summary>
    /// Google OAuth provider
    /// </summary>
    Google = 1,
    
    /// <summary>
    /// Facebook OAuth provider (future support)
    /// </summary>
    Facebook = 2,
    
    /// <summary>
    /// GitHub OAuth provider (future support)
    /// </summary>
    GitHub = 3,
    
    /// <summary>
    /// Apple OAuth provider (future support)
    /// </summary>
    Apple = 4
}
