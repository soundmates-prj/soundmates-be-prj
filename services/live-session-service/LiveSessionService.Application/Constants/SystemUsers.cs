namespace LiveSessionService.Application.Constants;

/// <summary>
/// System user IDs for automated operations
/// </summary>
public static class SystemUsers
{
    /// <summary>
    /// System user for AzuraCast sync operations
    /// </summary>
    public static readonly Guid AzuraCastSyncUser = new Guid("00000000-0000-0000-0000-000000000001");
    
    /// <summary>
    /// System user for background jobs
    /// </summary>
    public static readonly Guid BackgroundJobUser = new Guid("00000000-0000-0000-0000-000000000002");
    
    /// <summary>
    /// System user for automated tasks
    /// </summary>
    public static readonly Guid SystemUser = new Guid("00000000-0000-0000-0000-000000000003");
}
