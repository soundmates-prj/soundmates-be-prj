using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Mappings;
using AuthService.Application.Results;
using AuthService.Application.Features.Auth.Commands;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces;
using Shared.Contracts;
using Shared.Contracts.Events.Auth;
using System;
using System.Threading;
using System.Threading.Tasks;

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
        var otp = await _otpRepository.GetByEmailAndCodeAsync(
            command.Email, command.OtpCode, OtpPurpose.EmailVerification);

        if (otp == null)
        {
            return Result<AuthResult>.Failure("Invalid or expired OTP code", 400);
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var user = await _repo.VerifyEmailAsync(command.Email, command.OtpCode);

            if (user == null)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
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

            // Enqueue user updated event BEFORE commit — outbox lives in same transaction
            await _outbox.EnqueueAsync(RoutingKeys.Auth.UserUpdated, new UserUpdatedEvent
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                RoleId = user.RoleId ?? Guid.Empty,
                RoleName = user.Role?.Name ?? "MEMBER",
                IsActive = true,
                UpdatedAt = _dateTimeProvider.UtcNow
            }, cancellationToken);

            await _unitOfWork.CommitAsync(cancellationToken);

            var authResult = user.ToAuthResult(accessToken, refreshToken);
            return Result<AuthResult>.Success(
                authResult,
                "Email verified successfully. Your account is now active.");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}