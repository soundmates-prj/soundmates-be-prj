using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Results;
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
/// Admin manually verifies a user's email and activates the account.
/// Does NOT require OTP — admin grants verification directly.
/// State transition: UNVERIFIED → VERIFIED + ACTIVE
/// </summary>
public sealed class VerifyUserEmailHandler : ICommandHandler<VerifyUserEmailCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IOutboxRepository _outbox;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<VerifyUserEmailHandler> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public VerifyUserEmailHandler(
        IUserRepository userRepository,
        IOutboxRepository outbox,
        IDateTimeProvider dateTimeProvider,
        ILogger<VerifyUserEmailHandler> logger,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _outbox = outbox;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(VerifyUserEmailCommand command, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(command.UserId);

        if (user is null)
            throw new UserNotFoundException($"User with ID {command.UserId} not found");

        try
        {
            user.VerifyEmail(_dateTimeProvider);
        }
        catch (InvalidUserStateException ex)
        {
            return Result<bool>.Failure(ex.Message, ex.StatusCode);
        }

        await _userRepository.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload user with role for event payload
        user = await _userRepository.GetByIdAsync(user.Id);

        // Publish UserEmailVerifiedEvent (Auth — account state change)
        await _outbox.EnqueueAsync(RoutingKeys.Auth.UserEmailVerified, new UserEmailVerifiedEvent
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            EmailVerifiedAt = user.EmailVerifiedAt ?? _dateTimeProvider.UtcNow,
            IsActive = true
        }, cancellationToken);

        // Publish UserUpdatedEvent so query-service syncs isActive=true
        // (UserEmailVerifiedEvent only syncs email verification fields;
        // UserUpdatedEvent carries isActive to be explicit and future-proof)
        await _outbox.EnqueueAsync(RoutingKeys.Auth.UserUpdated, new UserUpdatedEvent
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName ?? string.Empty,
            LastName = user.LastName ?? string.Empty,
            RoleId = user.RoleId ?? Guid.Empty,
            RoleName = user.Role?.Name ?? "MEMBER",
            IsActive = true,
            UpdatedAt = _dateTimeProvider.UtcNow
        }, cancellationToken);

        _logger.LogInformation("Admin manually verified email for user {UserId}", user.Id);

        return Result<bool>.Success(true,
            $"Email of user '{user.Username}' has been verified and account activated");
    }
}

