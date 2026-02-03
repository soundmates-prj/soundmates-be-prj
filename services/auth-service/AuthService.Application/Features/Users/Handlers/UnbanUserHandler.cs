using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Results;
using AuthService.Application.Features.Users.Commands;
using AuthService.Domain.Exceptions;
using AuthService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Features.Users.Handlers;

public sealed class UnbanUserHandler : ICommandHandler<UnbanUserCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IOutboxRepository _outbox;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<UnbanUserHandler> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public UnbanUserHandler(
        IUserRepository userRepository,
        IOutboxRepository outbox,
        IDateTimeProvider dateTimeProvider,
        ILogger<UnbanUserHandler> logger,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _outbox = outbox;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(UnbanUserCommand command, CancellationToken cancellationToken)
    {
        // Get user
        var user = await _userRepository.GetByIdAsync(command.UserId);
        
        if (user is null)
        {
            throw new UserNotFoundException($"User with ID {command.UserId} not found");
        }

        // Use domain method to activate (unban)
        try
        {
            user.Activate(_dateTimeProvider);
        }
        catch (InvalidUserStateException ex)
        {
            // Already active
            return Result<bool>.Failure("User is not banned", ex.StatusCode);
        }

        await _userRepository.UpdateAsync(user);
        
        // Commit transaction
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload user with role
        user = await _userRepository.GetByIdAsync(user.Id);

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
            unbannedAt = _dateTimeProvider.UtcNow
        }, cancellationToken);

        _logger.LogInformation("User {UserId} has been unbanned", user.Id);

        return Result<bool>.Success(true, $"User '{user.Username}' has been unbanned successfully");
    }
}
