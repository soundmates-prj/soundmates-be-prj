using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.DTOs.Response;
using AuthService.Application.Services.Common;
using AuthService.Application.Services.Users.Commands;
using AuthService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Services.Users.Handlers;

public sealed class BanUserHandler : ICommandHandler<BanUserCommand, bool>
{
    private readonly IAccountStatusService _accountStatusService;
    private readonly IOutbox _outbox;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<BanUserHandler> _logger;

    public BanUserHandler(
        IAccountStatusService accountStatusService,
        IOutbox outbox,
        IDateTimeProvider dateTimeProvider,
        ILogger<BanUserHandler> logger)
    {
        _accountStatusService = accountStatusService;
        _outbox = outbox;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(BanUserCommand command, CancellationToken cancellationToken)
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
            return ApiResponse<bool>.FailureResponse("User is already banned", 400);
        }

        // Publish user banned event (semantic: this is a BAN, not just deactivation)
        await _outbox.EnqueueAsync("auth.user.banned", new
        {
            id = user.Id,
            username = user.Username,
            email = user.Email,
            firstName = user.FirstName,
            lastName = user.LastName,
            roleId = user.RoleId,
            roleName = user.Role?.Name,
            isActive = user.IsActive,
            reason = command.Reason,
            bannedAt = _dateTimeProvider.UtcNow
        }, cancellationToken);

        _logger.LogInformation("User {UserId} has been banned. Reason: {Reason}", user.Id, command.Reason ?? "No reason provided");

        return ApiResponse<bool>.SuccessResponse(true, $"User '{user.Username}' has been banned successfully");
    }
}
