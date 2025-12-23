using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.DTOs.Response;
using AuthService.Application.Services.Auth.Commands;
using AuthService.Application.Services.Common;
using AuthService.Domain.Interfaces;
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

        public ChangePasswordHandler(
            IUserRepository userRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IOutbox outbox,
            IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _outbox = outbox;
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<bool>> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
        {
            // Validate new password
            var (isValid, errorMessage) = PasswordValidator.Validate(command.NewPassword);
            if (!isValid)
            {
                return ApiResponse<bool>.FailureResponse(errorMessage!, 400);
            }

            // Get user
            var user = await _userRepository.GetByIdAsync(command.UserId);
            if (user == null)
            {
                return ApiResponse<bool>.FailureResponse("User not found", 404);
            }

            // Verify old password
            var isOldPasswordValid = BCrypt.Net.BCrypt.Verify(command.OldPassword, user.Password);
            if (!isOldPasswordValid)
            {
                return ApiResponse<bool>.FailureResponse("Old password is incorrect", 400);
            }

            // Check if new password is same as old password
            var isSamePassword = BCrypt.Net.BCrypt.Verify(command.NewPassword, user.Password);
            if (isSamePassword)
            {
                return ApiResponse<bool>.FailureResponse("New password must be different from old password", 400);
            }

            // Hash new password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(command.NewPassword);
            user.Password = passwordHash;

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
    }
}

