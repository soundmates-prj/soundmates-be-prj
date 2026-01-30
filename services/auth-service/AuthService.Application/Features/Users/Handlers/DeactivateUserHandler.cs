using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Results;
using AuthService.Application.Features.Users.Commands;
using AuthService.Domain.Exceptions;
using AuthService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Features.Users.Handlers;

/// <summary>
/// Soft delete user account - Sets IsActive to false (recommended approach)
/// This is different from hard DELETE which permanently removes the user
/// Uses domain methods directly following Clean Architecture
/// </summary>
public sealed class DeactivateUserHandler : ICommandHandler<DeactivateUserCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IOutboxRepository _outbox;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<DeactivateUserHandler> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateUserHandler(
        IUserRepository userRepository,
        IOutboxRepository outbox,
        IDateTimeProvider dateTimeProvider,
        ILogger<DeactivateUserHandler> logger,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _outbox = outbox;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(DeactivateUserCommand command, CancellationToken cancellationToken)
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
            return Result<bool>.Failure(ex.Message, ex.StatusCode);
        }

        await _userRepository.UpdateAsync(user);
        
        // Commit transaction
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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

        return Result<bool>.Success(true, "Your account has been deactivated successfully");
    }
}
