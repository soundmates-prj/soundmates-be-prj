using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.DTOs.Response;
using AuthService.Application.Services.Auth.Commands;
using AuthService.Application.Services.Common;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces;

namespace AuthService.Application.Services.Auth.Handlers
{
    public sealed class ForgetPasswordRequestHandler : ICommandHandler<ForgetPasswordRequestCommand, bool>
    {
        private readonly IUserRepository _userRepository;
        private readonly IOtpService _otpService;
        private readonly IUnitOfWork _unitOfWork;

        public ForgetPasswordRequestHandler(
            IUserRepository userRepository,
            IOtpService otpService,
            IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _otpService = otpService;
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<bool>> Handle(ForgetPasswordRequestCommand command, CancellationToken cancellationToken)
        {
            // Check if user exists
            var user = await _userRepository.GetByEmailAsync(command.Email);
            if (user == null)
            {
                // Don't reveal if email exists for security
                return ApiResponse<bool>.SuccessResponse(true, "If the email exists, an OTP code has been sent");
            }

            try
            {
                // Use shared OTP service to generate and send password reset OTP
                await _otpService.GenerateAndSendOtpAsync(
                    user.Email,
                    user.Username,
                    user.FirstName,
                    OtpPurpose.PasswordReset,
                    cancellationToken);
                
                return ApiResponse<bool>.SuccessResponse(true, "OTP code has been sent to your email");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.FailureResponse($"Failed to send email: {ex.Message}", 500);
            }
        }
    }
}

