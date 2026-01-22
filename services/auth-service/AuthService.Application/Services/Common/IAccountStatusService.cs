using AuthService.Domain.Entities;

namespace AuthService.Application.Services.Common;

/// <summary>
/// Service for managing account status changes (ban, deactivate, suspend)
/// Extracts common logic while keeping semantic clarity in handlers
/// </summary>
public interface IAccountStatusService
{
    /// <summary>
    /// Deactivates user account (sets IsActive = false)
    /// </summary>
    Task<User?> DeactivateAccountAsync(Guid userId);
    
    /// <summary>
    /// Activates user account (sets IsActive = true)
    /// </summary>
    Task<User?> ActivateAccountAsync(Guid userId);
    
    /// <summary>
    /// Checks if user account can be deactivated
    /// </summary>
    Task<(bool CanDeactivate, string? ErrorMessage)> CanDeactivateAccountAsync(Guid userId);
}
