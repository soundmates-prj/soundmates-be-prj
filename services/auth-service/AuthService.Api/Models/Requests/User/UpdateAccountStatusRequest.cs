using System.ComponentModel.DataAnnotations;
using AuthService.Application.Enums;

namespace AuthService.Api.Models.Requests.User;

/// <summary>
/// Request model for updating account status (PATCH semantics).
/// Idempotent — setting the same status twice is a no-op.
/// Replaces: /deactivate, /activate, /ban, /unban
/// </summary>
public class UpdateAccountStatusRequest
{
    /// <summary>
    /// Target account status.
    /// ACTIVE = account can login; DEACTIVATED/SUSPENDED = account is locked.
    /// </summary>
    [Required(ErrorMessage = "Status is required")]
    public AccountStatusEnum Status { get; set; }

    /// <summary>
    /// Optional reason for the status change.
    /// Examples: "USER_REQUEST", "POLICY_VIOLATION", "SUSPICIOUS_ACTIVITY"
    /// </summary>
    [StringLength(100)]
    public string? Reason { get; set; }

    /// <summary>
    /// Optional admin note describing the action in detail.
    /// Not visible to the user.
    /// </summary>
    [StringLength(500)]
    public string? Note { get; set; }
}
