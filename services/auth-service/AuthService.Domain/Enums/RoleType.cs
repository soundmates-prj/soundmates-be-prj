namespace AuthService.Domain.Enums;

/// <summary>
/// Role types in the system.
/// Defines different user roles and their access levels.
/// Must match the role names seeded in the database:
/// MEMBER, HOST, STAFF, ADMIN
/// </summary>
public enum RoleType
{
    /// <summary>
///     Regular member with basic access
///     </summary>
    MEMBER = 1,

    /// <summary>
    /// Host with live-session and podcast management access
    /// </summary>
    HOST = 2,

    /// <summary>
    /// Staff with content and system management access
    /// </summary>
    STAFF = 3,

    /// <summary>
    /// Administrator with full access
    /// </summary>
    ADMIN = 4
}
