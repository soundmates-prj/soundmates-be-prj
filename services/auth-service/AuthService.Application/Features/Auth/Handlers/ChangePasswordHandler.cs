using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Exceptions;
using AuthService.Application.Results;
using AuthService.Application.Features.Auth.Commands;
using AuthService.Domain.Exceptions;
using AuthService.Domain.Interfaces;
using AuthService.Domain.Rules;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AuthService.Application.Features.Auth.Handlers
{
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
                // Validate new password using Domain Rule
                PasswordRule.Validate(command.NewPassword);

                // Get user
                var user = await _userRepository.GetByIdAsync(command.UserId);
                if (user == null)
                {
                    throw new UserNotFoundException($"User with ID {command.UserId} not found");
                }

                // Verify old password
                var isOldPasswordValid = BCrypt.Net.BCrypt.Verify(command.OldPassword, user.Password);
                if (!isOldPasswordValid)
                {
                    return Result<bool>.Failure("Old password is incorrect", 401);
                }

                // Check if new password is same as old password
                var isSamePassword = BCrypt.Net.BCrypt.Verify(command.NewPassword, user.Password);
                if (isSamePassword)
                {
                    return Result<bool>.Failure("New password must be different from old password", 400);
                }

                // Hash new password and use domain method
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(command.NewPassword);
                user.ChangePassword(passwordHash, _dateTimeProvider);

                // Invalidate all refresh tokens for security
                await _refreshTokenRepository.RevokeAllUserTokensAsync(user.Id);

                await _userRepository.UpdateAsync(user);
                
                // Commit transaction
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Reload user with role to get latest data
                user = await _userRepository.GetByIdAsync(user.Id);

                // Publish event - sync user data to query service (password not included in read model)
                await _outbox.EnqueueAsync("auth.user.updated", new
                {
                    id = user.Id,
                    username = user.Username,
                    email = user.Email,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    roleId = user.RoleId,
                    roleName = user.Role?.Name
                }, cancellationToken);

                return Result<bool>.Success(true, "Password has been changed successfully");
            }
            catch (UserValidationException ex)
            {
                // Domain validation errors
                return Result<bool>.Failure(ex.Message, ex.StatusCode);
            }
            catch (UserNotFoundException ex)
            {
                // User not found
                return Result<bool>.Failure(ex.Message, ex.StatusCode);
            }
            catch (AuthException ex)
            {
                // Application auth errors
                return Result<bool>.Failure(ex.Message, 400);
            }
        }
    }
}

