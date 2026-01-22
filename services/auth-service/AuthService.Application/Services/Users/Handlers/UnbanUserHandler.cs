using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.DTOs.Response;
using AuthService.Application.Services.Common;
using AuthService.Application.Services.Users.Commands;
using AuthService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Services.Users.Handlers;

public sealed class UnbanUserHandler : ICommandHandler<UnbanUserCommand, bool>
{
    private readonly IAccountStatusService _accountStatusService;
    private readonly IOutbox _outbox;
    private readonly ILogger<UnbanUserHandler> _logger;

    public UnbanUserHandler(
        IAccountStatusService accountStatusService,
        IOutbox outbox,
        ILogger<UnbanUserHandler> logger)
    {
        _accountStatusService = accountStatusService;
        _outbox = outbox;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(UnbanUserCommand command, CancellationToken cancellationToken)
    {
        // Use shared service to activate account
        var user = await _accountStatusService.ActivateAccountAsync(command.UserId);
        
        if (user is null)
        {
            return ApiResponse<bool>.FailureResponse("User not found", 404);
        }

        if (!user.IsActive)
        {
            // Service returned user but IsActive is still false (was already inactive)
            return ApiResponse<bool>.FailureResponse("User is not banned", 400);
        }

        // Publish user unbanned event (semantic: this is an UNBAN, not just activation)
        await _outbox.EnqueueAsync("auth.user.unbanned", new
        {
            id = user.Id,
            username = user.Username,
            email = user.Email,
            firstName = user.FirstName,
            lastName = user.LastName,
            roleId = user.RoleId,
            roleName = user.Role?.Name,
            isActive = user.IsActive,
            unbannedAt = DateTime.UtcNow
        }, cancellationToken);

        _logger.LogInformation("User {UserId} has been unbanned", user.Id);

        return ApiResponse<bool>.SuccessResponse(true, $"User '{user.Username}' has been unbanned successfully");
    }
}
