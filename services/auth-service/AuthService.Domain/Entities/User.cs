using System;
using System.Collections.Generic;

namespace AuthService.Domain.Entities;

public partial class User
{
    public Guid Id { get; set; }

    public Guid? RoleId { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsActive { get; set; } = false;

    /// <summary>
    /// Indicates whether the account is temporarily locked due to too many failed login attempts.
    /// Lockout is automatic after 5 consecutive failed attempts; auto-unlocks after 15 minutes.
    /// </summary>
    public bool IsLocked { get; set; } = false;

    /// <summary>
    /// Timestamp when the account was locked (null if not locked).
    /// </summary>
    public DateTime? LockedAt { get; set; }

    /// <summary>
    /// Number of consecutive failed login attempts since the last successful login.
    /// Resets to 0 on successful login or after lockout auto-expires.
    /// </summary>
    public int FailedLoginAttempts { get; set; } = 0;

    public string? EmailVerificationToken { get; set; }

    public DateTime? EmailVerifiedAt { get; set; }

    /// <summary>
    /// Reason for deactivation (set when member self-deactivates).
    /// Values: "Tạm nghỉ", "Quá nhiều thông báo", "Lý do cá nhân", "Khác"
    /// </summary>
    public string? DeactivationReason { get; set; }

    /// <summary>
    /// When the member requested permanent deletion.
    /// </summary>
    public DateTime? DeletionRequestedAt { get; set; }

    /// <summary>
    /// Scheduled permanent deletion date = DeletionRequestedAt + 30 days.
    /// After this date, a background job permanently erases the account.
    /// </summary>
    public DateTime? DeletionScheduledAt { get; set; }

    public virtual ICollection<Oauthaccount> Oauthaccounts { get; set; } = new List<Oauthaccount>();

    public virtual UserRole? Role { get; set; }
    
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    
    public virtual Profile? Profile { get; set; }

    public virtual ICollection<UserFavourite> UserFavourites { get; set; } = new List<UserFavourite>();

    public virtual SpotifyToken? SpotifyToken { get; set; }

    public virtual BankAccount? BankAccount { get; set; }
}
