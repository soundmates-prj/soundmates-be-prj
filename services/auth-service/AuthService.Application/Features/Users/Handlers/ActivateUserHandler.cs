using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Results;
using AuthService.Application.Features.Users.Commands;
using AuthService.Domain.Exceptions;
using AuthService.Domain.Interfaces;
using Shared.Contracts;
using Shared.Contracts.Events.Auth;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AuthService.Application.Features.Users.Handlers;

/// <summary>
/// Activates a previously deactivated user account.
/// Note: UpdateAccountStatusHandler is preferred for new code as it is idempotent
/// and unifies all status transitions.
/// </summary>
public sealed class ActivateUserHandler : ICommandHandler<ActivateUserCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IOutboxRepository _outbox;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public ActivateUserHandler(
        IUserRepository userRepository,
        IOutboxRepository outbox,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _outbox = outbox;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(ActivateUserCommand command, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(command.UserId);
        if (user is null)
            throw new UserNotFoundException($"User with ID {command.UserId} not found");

        try
        {
            user.Activate(_dateTimeProvider);
        }
        catch (InvalidUserStateException ex)
        {
            return Result<bool>.Failure(ex.Message, ex.StatusCode);
        }

        await _userRepository.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        user = await _userRepository.GetByIdAsync(user.Id);

        // Publish typed UserActivatedEvent (Auth — domain state change)
        await _outbox.EnqueueAsync(RoutingKeys.Auth.UserActivated, new UserActivatedEvent
        {
            UserId = user!.Id,
            Username = user.Username,
            Email = user.Email
        }, cancellationToken);

        return Result<bool>.Success(true, $"User '{user.Username}' has been activated successfully");
    }
}
