namespace AuthService.Domain.Enums;

/// <summary>
/// Role types in the system
/// Defines different user roles and their access levels
/// </summary>
public enum RoleType
{
    /// <summary>
    /// Regular member with basic access
    /// </summary>
    MEMBER = 1,
    
    /// <summary>
    /// Administrator with full access
    /// </summary>
    ADMIN = 2,
    
    /// <summary>
    /// Moderator with content management access
    /// </summary>
    MODERATOR = 3
}
