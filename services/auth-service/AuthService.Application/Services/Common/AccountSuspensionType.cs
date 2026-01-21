namespace AuthService.Application.Services.Common;

/// <summary>
/// Represents the type of account suspension
/// </summary>
public enum AccountSuspensionType
{
    /// <summary>
    /// Account banned by admin (policy violation)
    /// </summary>
    Banned,
    
    /// <summary>
    /// Account deactivated by user or admin (soft delete)
    /// </summary>
    Deactivated,
    
    /// <summary>
    /// Account suspended temporarily (future use)
    /// </summary>
    Suspended
}
