namespace AuthService.Application.Enums;

/// <summary>
/// User account status enum
/// </summary>
public enum AccountStatusEnum
{
    /// <summary>Account is active and can login</summary>
    Active = 1,

    /// <summary>Account is temporarily deactivated (self or admin)</summary>
    Deactivated = 2,

    /// <summary>Account is suspended due to policy violation</summary>
    Suspended = 3,

    /// <summary>Account is pending permanent deletion (30-day grace period)</summary>
    PendingDeletion = 4
}
