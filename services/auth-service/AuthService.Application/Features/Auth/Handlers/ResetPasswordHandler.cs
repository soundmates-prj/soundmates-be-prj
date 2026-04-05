using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Results;
using AuthService.Application.Features.Auth.Commands;
using AuthService.Domain.Enums;
using AuthService.Domain.Exceptions;
using AuthService.Domain.Interfaces;
using AuthService.Domain.Rules;
using Shared.Contracts;
using Shared.Contracts.Events.Auth;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AuthService.Application.Features.Auth.Handlers;

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
        try
        {
            PasswordRule.Validate(command.NewPassword);
        }
        catch (UserValidationException ex)
        {
            return Result<bool>.Failure(ex.Message, ex.StatusCode);
        }

        var otp = await _otpRepository.GetByEmailAndCodeAsync(
            command.Email, command.OtpCode, OtpPurpose.PasswordReset);

        if (otp == null)
            return Result<bool>.Failure("Invalid or expired OTP code", 400);

        var user = await _userRepository.GetByEmailAsync(command.Email);
        if (user == null)
            return Result<bool>.Failure("User not found", 404);

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(command.NewPassword);
        user.ChangePassword(passwordHash, _dateTimeProvider);

        otp.IsUsed = true;
        otp.UsedAt = _dateTimeProvider.UtcNow;

        // Invalidate all refresh tokens for security
        await _refreshTokenRepository.RevokeAllUserTokensAsync(user.Id);

        await _userRepository.UpdateAsync(user);
        await _otpRepository.UpdateAsync(otp);
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

        return Result<bool>.Success(true, "Password has been reset successfully");
    }
}
