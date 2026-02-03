using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Results;
using AuthService.Application.Features.Auth.Commands;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Domain.Exceptions;
using AuthService.Domain.Interfaces;
using AuthService.Domain.Rules;

namespace AuthService.Application.Features.Auth.Handlers
{
    public sealed class ResetPasswordHandler : ICommandHandler<ResetPasswordCommand, bool>
    {
        private readonly IUserRepository _userRepository;
        private readonly IOtpRepository _otpRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IOutboxRepository _outbox;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDateTimeProvider _dateTimeProvider;

        public ResetPasswordHandler(
            IUserRepository userRepository,
            IOtpRepository otpRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IOutboxRepository outbox,
            IUnitOfWork unitOfWork,
            IDateTimeProvider dateTimeProvider)
        {
            _userRepository = userRepository;
            _otpRepository = otpRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _outbox = outbox;
            _unitOfWork = unitOfWork;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<Result<bool>> Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
        {
            // Validate password using Domain Rule
            try
            {
                PasswordRule.Validate(command.NewPassword);
            }
            catch (UserValidationException ex)
            {
                return Result<bool>.Failure(ex.Message, ex.StatusCode);
            }

            // Verify OTP
            var otp = await _otpRepository.GetByEmailAndCodeAsync(
                command.Email, 
                command.OtpCode, 
                OtpPurpose.PasswordReset);

            if (otp == null)
            {
                return Result<bool>.Failure("Invalid or expired OTP code", 400);
            }

            // Get user
            var user = await _userRepository.GetByEmailAsync(command.Email);
            if (user == null)
            {
                return Result<bool>.Failure("User not found", 404);
            }

            // Hash new password and use domain method
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(command.NewPassword);
            user.ChangePassword(passwordHash, _dateTimeProvider);

            // Mark OTP as used
            otp.IsUsed = true;
            otp.UsedAt = _dateTimeProvider.UtcNow;

            // Invalidate all refresh tokens for security
            await _refreshTokenRepository.RevokeAllUserTokensAsync(user.Id);

            await _userRepository.UpdateAsync(user);
            await _otpRepository.UpdateAsync(otp);
            
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

            return Result<bool>.Success(true, "Password has been reset successfully");
        }
    }
}

