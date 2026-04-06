using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Enums;
using Microsoft.Extensions.Logging;
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
/// Unified handler for all account status operations.
/// PATCH semantics — idempotent: setting the same status twice is a no-op.
/// Publishes the corresponding typed event to the outbox.
/// </summary>
public sealed class UpdateAccountStatusHandler : ICommandHandler<UpdateAccountStatusCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IOutboxRepository _outbox;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<UpdateAccountStatusHandler> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateAccountStatusHandler(
        IUserRepository userRepository,
        IOutboxRepository outbox,
        IDateTimeProvider dateTimeProvider,
        ILogger<UpdateAccountStatusHandler> logger,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _outbox = outbox;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public async Task<Application.Results.Result<bool>> Handle(
        UpdateAccountStatusCommand command,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(command.UserId);
        if (user is null)
            throw new UserNotFoundException($"User with ID {command.UserId} not found");

        var oldStatus = MapToStatus(user);
        var newStatus = command.Status;

        // Idempotent: already in target status → no-op
        if (oldStatus == newStatus)
        {
            _logger.LogInformation(
                "User {UserId} already in status {Status}, skipping update",
                user.Id, newStatus);
            return Application.Results.Result<bool>.Success(true,
                $"User already in '{newStatus}' status");
        }

        try
        {
            switch (newStatus)
            {
                case AccountStatusEnum.Active:
                    // If account is pending deletion, cancel it first
                    if (user.DeletionScheduledAt.HasValue)
                        user.CancelDeletionRequest(_dateTimeProvider);
                    else
                        user.Activate(_dateTimeProvider);
                    break;

                case AccountStatusEnum.Deactivated:
                case AccountStatusEnum.Suspended:
                    user.Deactivate(_dateTimeProvider);
                    break;

                case AccountStatusEnum.PendingDeletion:
                    user.MarkForDeletion(_dateTimeProvider);
                    break;

                default:
                    return Application.Results.Result<bool>.Failure("Invalid status transition", 400);
            }
        }
        catch (InvalidUserStateException ex)
        {
            return Application.Results.Result<bool>.Failure(ex.Message, ex.StatusCode);
        }

        await _userRepository.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        user = await _userRepository.GetByIdAsync(user.Id);

        // Publish typed event based on new status
        var now = _dateTimeProvider.UtcNow;
        var routingKey = newStatus switch
        {
            AccountStatusEnum.Active           => RoutingKeys.Auth.UserActivated,
            AccountStatusEnum.Deactivated      => RoutingKeys.Auth.UserDeactivated,
            AccountStatusEnum.Suspended        => RoutingKeys.Auth.UserBanned,
            AccountStatusEnum.PendingDeletion  => RoutingKeys.Auth.UserDeleted,
            _ => throw new InvalidOperationException($"Unknown status: {newStatus}")
        };

        object evt = newStatus switch
        {
            AccountStatusEnum.Active => new UserActivatedEvent
            {
                UserId = user!.Id,
                Username = user.Username,
                Email = user.Email
            },
            AccountStatusEnum.Deactivated => new UserDeactivatedEvent
            {
                UserId = user!.Id,
                Username = user.Username,
                Email = user.Email,
                DeactivatedAt = now
            },
            AccountStatusEnum.Suspended => new UserBannedEvent
            {
                UserId = user!.Id,
                Username = user.Username,
                Email = user.Email,
                Reason = command.Reason,
                BannedAt = now
            },
            AccountStatusEnum.PendingDeletion => new UserDeletedEvent
            {
                Id = user!.Id,
                DeletedAt = user.DeletionScheduledAt ?? now.AddDays(30),
                Reason = command.Reason ?? "Admin marked account for deletion"
            },
            _ => throw new InvalidOperationException($"Unknown status: {newStatus}")
        };

        await _outbox.EnqueueAsync(routingKey, evt, cancellationToken);

        _logger.LogInformation(
            "User {UserId} status changed: {OldStatus} → {NewStatus}. Reason: {Reason}",
            user.Id, oldStatus, newStatus, command.Reason ?? "N/A");

        var message = newStatus switch
        {
            AccountStatusEnum.Active      => $"User '{user!.Username}' activated successfully",
            AccountStatusEnum.Deactivated  => $"User '{user!.Username}' deactivated successfully",
            AccountStatusEnum.Suspended    => $"User '{user!.Username}' suspended successfully",
            _ => $"User '{user!.Username}' status updated"
        };

        return Application.Results.Result<bool>.Success(true, message);
    }

    private static AccountStatusEnum MapToStatus(Domain.Entities.User user)
    {
        if (user.IsActive)
            return AccountStatusEnum.Active;
        // Pending deletion takes precedence over deactivated/suspended
        if (user.DeletionScheduledAt.HasValue && user.DeletionScheduledAt > DateTime.UtcNow)
            return AccountStatusEnum.PendingDeletion;
        return AccountStatusEnum.Deactivated;
    }
}
