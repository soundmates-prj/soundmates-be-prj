using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Results;
using AuthService.Application.Features.Auth.Commands;
using AuthService.Application.Features.Common;
using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces;

namespace AuthService.Application.Features.Auth.Handlers;

public sealed class ForgetPasswordHandler : ICommandHandler<ForgetPasswordCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IOtpService _otpService;
    private readonly IUnitOfWork _unitOfWork;

    public ForgetPasswordHandler(
        IUserRepository userRepository,
        IOtpService otpService,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _otpService = otpService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(ForgetPasswordCommand command, CancellationToken cancellationToken)
    {
        // Check if user exists
        var user = await _userRepository.GetByEmailAsync(command.Email);
        if (user == null)
        {
            // Don't reveal if email exists for security
            return Result<bool>.Success(true, "If the email exists, an OTP code has been sent");
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
            
            return Result<bool>.Success(true, "OTP code has been sent to your email");
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure($"Failed to send email: {ex.Message}", 500);
        }
    }
}

