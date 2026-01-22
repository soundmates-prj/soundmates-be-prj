using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.DTOs.Response;
using AuthService.Application.Services.Common;
using AuthService.Application.Services.Users.Commands;
using AuthService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Services.Users.Handlers;

/// <summary>
/// Soft delete user account - Sets IsActive to false (recommended approach)
/// This is different from hard DELETE which permanently removes the user
/// Uses shared AccountStatusService to reduce code duplication
/// </summary>
public sealed class DeactivateUserHandler : ICommandHandler<DeactivateUserCommand, bool>
{
    private readonly IAccountStatusService _accountStatusService;
    private readonly IOutbox _outbox;
    private readonly ILogger<DeactivateUserHandler> _logger;

    public DeactivateUserHandler(
        IAccountStatusService accountStatusService,
        IOutbox outbox,
        ILogger<DeactivateUserHandler> logger)
    {
        _accountStatusService = accountStatusService;
        _outbox = outbox;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(DeactivateUserCommand command, CancellationToken cancellationToken)
    {
        // Use shared service to deactivate account
        var user = await _accountStatusService.DeactivateAccountAsync(command.UserId);
        
        if (user is null)
        {
            return ApiResponse<bool>.FailureResponse("User not found", 404);
        }

        if (user.IsActive)
        {
            // Service returned user but IsActive is still true (was already inactive)
            return ApiResponse<bool>.FailureResponse("User account is already deactivated", 400);
        }

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
            deactivatedAt = DateTime.UtcNow
        }, cancellationToken);

        _logger.LogInformation("User {UserId} account has been deactivated (soft delete)", user.Id);

        return ApiResponse<bool>.SuccessResponse(true, "Your account has been deactivated successfully");
    }
}
