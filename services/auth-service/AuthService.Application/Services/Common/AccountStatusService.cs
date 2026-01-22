using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Services.Common;

/// <summary>
/// Shared service for account status management
/// Reduces code duplication between Ban, Unban, Deactivate handlers
/// </summary>
public class AccountStatusService : IAccountStatusService
{
    private readonly IUserRepository _userRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<AccountStatusService> _logger;

    public AccountStatusService(
        IUserRepository userRepository,
        IDateTimeProvider dateTimeProvider,
        ILogger<AccountStatusService> logger)
    {
        _userRepository = userRepository;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<User?> DeactivateAccountAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
        {
            _logger.LogWarning("Cannot deactivate account: User {UserId} not found", userId);
            return null;
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Cannot deactivate account: User {UserId} is already inactive", userId);
            return user; // Return user but caller should check IsActive
        }

        // Use domain method instead of directly setting properties
        user.Deactivate(_dateTimeProvider);

        await _userRepository.UpdateAsync(user);

        // Reload with role
        user = await _userRepository.GetByIdAsync(user.Id);

        _logger.LogInformation("Account {UserId} deactivated successfully", userId);

        return user;
    }

    public async Task<User?> ActivateAccountAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
        {
            _logger.LogWarning("Cannot activate account: User {UserId} not found", userId);
            return null;
        }

        if (user.IsActive)
        {
            _logger.LogWarning("Cannot activate account: User {UserId} is already active", userId);
            return user; // Return user but caller should check IsActive
        }

        // Use domain method instead of directly setting properties
        user.Activate(_dateTimeProvider);

        await _userRepository.UpdateAsync(user);

        // Reload with role
        user = await _userRepository.GetByIdAsync(user.Id);

        _logger.LogInformation("Account {UserId} activated successfully", userId);

        return user;
    }

    public async Task<(bool CanDeactivate, string? ErrorMessage)> CanDeactivateAccountAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        
        if (user is null)
        {
            return (false, "User not found");
        }

        if (!user.IsActive)
        {
            return (false, "Account is already inactive");
        }

        // Add more business rules here if needed:
        // - Check if user has pending orders
        // - Check if user has active subscriptions
        // - Check if user is admin (maybe prevent deactivation)
        
        return (true, null);
    }
}
