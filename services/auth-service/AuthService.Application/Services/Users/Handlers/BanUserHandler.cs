using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.DTOs.Response;
using AuthService.Application.Services.Users.Commands;
using AuthService.Domain.Exceptions;
using AuthService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Services.Users.Handlers;

public sealed class BanUserHandler : ICommandHandler<BanUserCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IOutbox _outbox;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<BanUserHandler> _logger;

    public BanUserHandler(
        IUserRepository userRepository,
        IOutbox outbox,
        IDateTimeProvider dateTimeProvider,
        ILogger<BanUserHandler> logger)
    {
        _userRepository = userRepository;
        _outbox = outbox;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(BanUserCommand command, CancellationToken cancellationToken)
    {
        // Get user
        var user = await _userRepository.GetByIdAsync(command.UserId);
        
        if (user is null)
        {
            throw new UserNotFoundException($"User with ID {command.UserId} not found");
        }

        // Use domain method to deactivate (ban is same as deactivate in our domain)
        try
        {
            user.Deactivate(_dateTimeProvider);
        }
        catch (InvalidUserStateException ex)
        {
            // Already inactive/banned
            return ApiResponse<bool>.FailureResponse("User is already banned", ex.StatusCode);
        }

        await _userRepository.UpdateAsync(user);

        // Reload user with role
        user = await _userRepository.GetByIdAsync(user.Id);

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
