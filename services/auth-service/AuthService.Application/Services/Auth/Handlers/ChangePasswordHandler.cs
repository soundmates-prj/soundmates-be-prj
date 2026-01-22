using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.DTOs.Response;
using AuthService.Application.Enums;
using AuthService.Application.Exceptions;
using AuthService.Application.Services.Auth.Commands;
using AuthService.Domain.Exceptions;
using AuthService.Domain.Interfaces;
using AuthService.Domain.Rules;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AuthService.Application.Services.Auth.Handlers
{
    public sealed class ChangePasswordHandler : ICommandHandler<ChangePasswordCommand, bool>
    {
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IOutbox _outbox;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDateTimeProvider _dateTimeProvider;

        public ChangePasswordHandler(
            IUserRepository userRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IOutbox outbox,
            IUnitOfWork unitOfWork,
            IDateTimeProvider dateTimeProvider)
        {
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _outbox = outbox;
            _unitOfWork = unitOfWork;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<ApiResponse<bool>> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
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
                    throw new AuthException(AuthErrorCode.OldPasswordIncorrect, "Old password is incorrect");
                }

                // Check if new password is same as old password
                var isSamePassword = BCrypt.Net.BCrypt.Verify(command.NewPassword, user.Password);
                if (isSamePassword)
                {
                    return ApiResponse<bool>.FailureResponse("New password must be different from old password", 400);
                }

                // Hash new password and use domain method
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(command.NewPassword);
                user.ChangePassword(passwordHash, _dateTimeProvider);

                // Invalidate all refresh tokens for security
                await _refreshTokenRepository.RevokeAllUserTokensAsync(user.Id);

                await _userRepository.UpdateAsync(user);

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

                return ApiResponse<bool>.SuccessResponse(true, "Password has been changed successfully");
            }
            catch (UserValidationException ex)
            {
                // Domain validation errors
                return ApiResponse<bool>.FailureResponse(ex.Message, ex.StatusCode);
            }
            catch (UserNotFoundException ex)
            {
                // User not found
                return ApiResponse<bool>.FailureResponse(ex.Message, ex.StatusCode);
            }
            catch (AuthException ex)
            {
                // Application auth errors
                return ApiResponse<bool>.FailureResponse(ex.Message, 400);
            }
        }
    }
}

