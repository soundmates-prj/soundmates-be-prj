using AuthService.Application.DTOs;
using AuthService.Application.DTOs.Response;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Services.Auth.Commands;

namespace AuthService.Application.Services.Auth.Handlers;

public sealed class VerifyEmailHandler : ICommandHandler<VerifyEmailCommand, UserDto>
{
    private readonly IAuthRepository _repo;
    private readonly IOtpRepository _otpRepository;
    private readonly IOutbox _outbox;
    private readonly IUnitOfWork _unitOfWork;

    public VerifyEmailHandler(
        IAuthRepository repo,
        IOtpRepository otpRepository,
        IOutbox outbox,
        IUnitOfWork unitOfWork)
    {
        _repo = repo;
        _otpRepository = otpRepository;
        _outbox = outbox;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<UserDto>> Handle(VerifyEmailCommand command, CancellationToken cancellationToken)
    {
        // Verify OTP
        var otp = await _otpRepository.GetByEmailAndCodeAsync(
            command.Email,
            command.OtpCode,
            OtpPurpose.EmailVerification);

        if (otp == null)
        {
            return ApiResponse<UserDto>.FailureResponse("Invalid or expired OTP code", 400);
        }

        // Verify email using OTP
        var user = await _repo.VerifyEmailAsync(command.Email, command.OtpCode);
        
        if (user == null)
        {
            return ApiResponse<UserDto>.FailureResponse("Invalid or expired OTP code", 400);
        }

        // Mark OTP as used
        otp.IsUsed = true;
        otp.UsedAt = DateTime.UtcNow;
        await _otpRepository.UpdateAsync(otp);

        // Publish user updated event
        await _outbox.EnqueueAsync("auth.user.updated", new
        {
            id = user.Id,
            username = user.Username,
            email = user.Email,
            firstName = user.FirstName,
            lastName = user.LastName,
            roleId = user.RoleId,
            roleName = user.Role?.Name,
            isActive = user.IsActive,
            emailVerifiedAt = user.EmailVerifiedAt,
            updatedAt = user.UpdatedAt
        }, cancellationToken);

        var dto = new UserDto(user);

        return ApiResponse<UserDto>.SuccessResponse(dto, "Email verified successfully. Your account is now active.");
    }
}

