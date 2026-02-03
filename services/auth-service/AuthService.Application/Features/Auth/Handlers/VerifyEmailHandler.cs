using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Mappings;
using AuthService.Application.Results;
using AuthService.Application.Features.Auth.Commands;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces;

namespace AuthService.Application.Features.Auth.Handlers;

public sealed class VerifyEmailHandler : ICommandHandler<VerifyEmailCommand, AuthResult>
{
    private readonly IAuthRepository _repo;
    private readonly IOtpRepository _otpRepository;
    private readonly IOutboxRepository _outbox;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IJwtTokenGenerator _jwt;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public VerifyEmailHandler(
        IAuthRepository repo,
        IOtpRepository otpRepository,
        IOutboxRepository outbox,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IJwtTokenGenerator jwt,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _repo = repo;
        _otpRepository = otpRepository;
        _outbox = outbox;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _jwt = jwt;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<Result<AuthResult>> Handle(VerifyEmailCommand command, CancellationToken cancellationToken)
    {
        // Verify OTP
        var otp = await _otpRepository.GetByEmailAndCodeAsync(
            command.Email,
            command.OtpCode,
            OtpPurpose.EmailVerification);

        if (otp == null)
        {
            return Result<AuthResult>.Failure("Invalid or expired OTP code", 400);
        }

        // Verify email using OTP
        var user = await _repo.VerifyEmailAsync(command.Email, command.OtpCode);
        
        if (user == null)
        {
            return Result<AuthResult>.Failure("Invalid or expired OTP code", 400);
        }

        // Mark OTP as used
        otp.IsUsed = true;
        otp.UsedAt = _dateTimeProvider.UtcNow;
        await _otpRepository.UpdateAsync(otp);
        
        // Generate tokens for immediate login after verification
        var (accessToken, refreshToken) = _jwt.GenerateTokenPair(user);
        
        // Save refresh token
        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = _dateTimeProvider.UtcNow.AddDays(7),
            CreatedAt = _dateTimeProvider.UtcNow,
            IsRevoked = false
        };
        await _refreshTokenRepository.AddAsync(refreshTokenEntity);
        
        // Commit transaction
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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

        // Map to AuthResult
        var authResult = user.ToAuthResult(accessToken, refreshToken);

        return Result<AuthResult>.Success(authResult, "Email verified successfully. Your account is now active.");
    }
}

