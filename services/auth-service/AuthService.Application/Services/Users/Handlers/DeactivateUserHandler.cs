using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.DTOs.Response;
using AuthService.Application.Services.Users.Commands;
using AuthService.Domain.Exceptions;
using AuthService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Services.Users.Handlers;

/// <summary>
/// Soft delete user account - Sets IsActive to false (recommended approach)
/// This is different from hard DELETE which permanently removes the user
/// Uses domain methods directly following Clean Architecture
/// </summary>
public sealed class DeactivateUserHandler : ICommandHandler<DeactivateUserCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IOutbox _outbox;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<DeactivateUserHandler> _logger;

    public DeactivateUserHandler(
        IUserRepository userRepository,
        IOutbox outbox,
        IDateTimeProvider dateTimeProvider,
        ILogger<DeactivateUserHandler> logger)
    {
        _userRepository = userRepository;
        _outbox = outbox;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(DeactivateUserCommand command, CancellationToken cancellationToken)
    {
        // Get user
        var user = await _userRepository.GetByIdAsync(command.UserId);
        
        if (user is null)
        {
            throw new UserNotFoundException($"User with ID {command.UserId} not found");
        }

        // Use domain method to deactivate
        try
        {
            user.Deactivate(_dateTimeProvider);
        }
        catch (InvalidUserStateException ex)
        {
            // Already inactive
            return ApiResponse<bool>.FailureResponse(ex.Message, ex.StatusCode);
        }

        await _userRepository.UpdateAsync(user);

        // Reload user with role
        user = await _userRepository.GetByIdAsync(user.Id);

        // Publish user deactivated event (semantic: this is a DEACTIVATION by user, not a ban)
        await _outbox.EnqueueAsync("auth.user.deactivated", new
        {
            id = user.Id,
            username = user.Username,
            email = user.Email,
            firstName = user.FirstName,
            lastName = user.LastName,
            roleId = user.RoleId,
            roleName = user.Role?.Name,
            isActive = user.IsActive,
            deactivatedAt = _dateTimeProvider.UtcNow
        }, cancellationToken);

        _logger.LogInformation("User {UserId} account has been deactivated (soft delete)", user.Id);

        return ApiResponse<bool>.SuccessResponse(true, "Your account has been deactivated successfully");
    }
}
