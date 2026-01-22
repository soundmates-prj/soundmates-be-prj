using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.DTOs.Response;
using AuthService.Application.Services.Auth.Commands;
using AuthService.Application.Services.Common;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Services.Auth.Handlers;

public sealed class ResendOtpHandler : ICommandHandler<ResendOtpCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IOtpRepository _otpRepository;
    private readonly IOtpService _otpService;
    private readonly ILogger<ResendOtpHandler> _logger;

    public ResendOtpHandler(
        IUserRepository userRepository,
        IOtpRepository otpRepository,
        IOtpService otpService,
        ILogger<ResendOtpHandler> logger)
    {
        _userRepository = userRepository;
        _otpRepository = otpRepository;
        _otpService = otpService;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(ResendOtpCommand command, CancellationToken cancellationToken)
    {
        // Check if user exists
        var user = await _userRepository.GetByEmailAsync(command.Email);
        if (user == null)
        {
            return ApiResponse<bool>.FailureResponse("Email not found", 404);
        }

        // Check if email is already verified
        if (user.EmailVerifiedAt.HasValue)
        {
            return ApiResponse<bool>.FailureResponse("Email is already verified", 400);
        }

        // Check for rate limiting - prevent spam
        var latestOtp = await _otpRepository.GetLatestByEmailAsync(command.Email, OtpPurpose.EmailVerification);
        if (latestOtp != null && latestOtp.CreatedAt > DateTime.UtcNow.AddMinutes(-1))
        {
            return ApiResponse<bool>.FailureResponse("Please wait at least 1 minute before requesting a new OTP code", 429);
        }

        try
        {
            // Use shared OTP service to generate and send OTP
            await _otpService.GenerateAndSendOtpAsync(
                user.Email,
                user.Username,
                user.FirstName,
                OtpPurpose.EmailVerification,
                cancellationToken);

            _logger.LogInformation("Verification OTP resent successfully to {Email}", user.Email);
            
            return ApiResponse<bool>.SuccessResponse(true, "A new verification code has been sent to your email");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verification email to {Email}", user.Email);
            return ApiResponse<bool>.FailureResponse("Failed to send verification email. Please try again later.", 500);
        }
    }
}
