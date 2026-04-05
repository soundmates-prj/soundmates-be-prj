using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Exceptions;
using AuthService.Application.Results;
using AuthService.Application.Features.Auth.Commands;
using AuthService.Domain.Exceptions;
using AuthService.Domain.Interfaces;
using AuthService.Domain.Rules;
using Shared.Contracts;
using Shared.Contracts.Events.Auth;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AuthService.Application.Features.Auth.Handlers;

public sealed class ChangePasswordHandler : ICommandHandler<ChangePasswordCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IOutboxRepository _outbox;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ChangePasswordHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IOutboxRepository outbox,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _outbox = outbox;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<bool>> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        try
        {
            PasswordRule.Validate(command.NewPassword);

            var user = await _userRepository.GetByIdAsync(command.UserId);
            if (user == null)
                throw new UserNotFoundException($"User with ID {command.UserId} not found");

            var isOldValid = BCrypt.Net.BCrypt.Verify(command.OldPassword, user.Password);
            if (!isOldValid)
                return Result<bool>.Failure("Old password is incorrect", 401);

            var isSame = BCrypt.Net.BCrypt.Verify(command.NewPassword, user.Password);
            if (isSame)
                return Result<bool>.Failure("New password must be different from old password", 400);

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(command.NewPassword);
            user.ChangePassword(passwordHash, _dateTimeProvider);

            // Invalidate all refresh tokens for security
            await _refreshTokenRepository.RevokeAllUserTokensAsync(user.Id);

            await _userRepository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            user = await _userRepository.GetByIdAsync(user.Id);

            // Publish typed UserUpdatedEvent (Auth — account data changed)
            await _outbox.EnqueueAsync(RoutingKeys.Auth.UserUpdated, new UserUpdatedEvent
            {
                Id = user!.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                RoleId = user.RoleId ?? Guid.Empty,
                RoleName = user.Role?.Name,
                IsActive = user.IsActive,
                UpdatedAt = DateTime.UtcNow
            }, cancellationToken);

            return Result<bool>.Success(true, "Password has been changed successfully");
        }
        catch (UserValidationException ex)
        {
            return Result<bool>.Failure(ex.Message, ex.StatusCode);
        }
        catch (UserNotFoundException ex)
        {
            return Result<bool>.Failure(ex.Message, ex.StatusCode);
        }
        catch (AuthException ex)
        {
            return Result<bool>.Failure(ex.Message, 400);
        }
    }
}
