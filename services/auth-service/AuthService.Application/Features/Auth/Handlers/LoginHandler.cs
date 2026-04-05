using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Mappings;
using AuthService.Application.Results;
using AuthService.Application.Features.Auth.Commands;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Shared.Contracts;
using Shared.Contracts.Events.Activity;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AuthService.Application.Features.Auth.Handlers;

public sealed class LoginHandler : ICommandHandler<LoginCommand, AuthResult>
{
    private const int MaxFailedAttempts = 5;

    private readonly IAuthRepository _repo;
    private readonly IJwtTokenGenerator _jwt;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IOutboxRepository _outbox;
    private readonly ILogger<LoginHandler> _logger;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public LoginHandler(
        IAuthRepository repo,
        IJwtTokenGenerator jwt,
        IRefreshTokenRepository refreshTokenRepository,
        IOutboxRepository outbox,
        ILogger<LoginHandler> logger,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _repo = repo;
        _jwt = jwt;
        _refreshTokenRepository = refreshTokenRepository;
        _outbox = outbox;
        _logger = logger;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AuthResult>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        // EF-01 / P3: Find user by identifier (separate from password check) so we can
        // track failed attempts even when the password is wrong.
        var user = await _repo.GetByUsernameOrEmailAsync(command.Identifier);

        if (user is null || !BCrypt.Net.BCrypt.Verify(command.Password, user.Password))
        {
            // Increment failed attempts for the account if it exists
            if (user != null)
            {
                user.FailedLoginAttempts++;

                // Lock the account after MaxFailedAttempts consecutive failures
                if (user.FailedLoginAttempts >= MaxFailedAttempts)
                {
                    user.IsLocked = true;
                    user.LockedAt = _dateTimeProvider.UtcNow;

                    _logger.LogWarning(
                        "Account {UserId} ({Email}) has been locked after {Attempts} failed login attempts",
                        user.Id, user.Email, user.FailedLoginAttempts);

                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    await _outbox.EnqueueAsync(RoutingKeys.Auth.LoginFailed, new LoginFailedEvent
                    {
                        Identifier = command.Identifier,
                        UserId = user.Id,
                        Reason = "Account locked due to too many failed attempts",
                        ErrorCode = 403
                    }, cancellationToken);

                    return Result<AuthResult>.Failure(
                        "Your account has been locked due to too many failed login attempts. Please try again in 15 minutes or contact support.",
                        403);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            await _outbox.EnqueueAsync(RoutingKeys.Auth.LoginFailed, new LoginFailedEvent
            {
                Identifier = command.Identifier,
                Reason = "Invalid username/email or password",
                ErrorCode = 401
            }, cancellationToken);

            return Result<AuthResult>.Failure("Invalid username/email or password", 401);
        }

        // User found & password verified — now check account status
        if (!user.IsActive)
        {
            // Check if account is pending deletion — user can still log in to cancel
            if (user.DeletionScheduledAt.HasValue && user.DeletionScheduledAt > _dateTimeProvider.UtcNow)
            {
                // User can still log in during grace period to cancel deletion
                return Result<AuthResult>.Failure(
                    $"Tài khoản đang chờ xóa. Hạn hủy: {user.DeletionScheduledAt:dd/MM/yyyy}. Đăng nhập để hủy yêu cầu.",
                    403);
            }

            // Distinguish between email not verified vs banned/deactivated
            var reason = !user.EmailVerifiedAt.HasValue
                ? "Email not verified"
                : "Account has been deactivated or banned";

            await _outbox.EnqueueAsync(RoutingKeys.Auth.LoginFailed, new LoginFailedEvent
            {
                Identifier = command.Identifier,
                UserId = user.Id,
                Reason = reason,
                ErrorCode = 403
            }, cancellationToken);

            return Result<AuthResult>.Failure(
                !user.EmailVerifiedAt.HasValue
                    ? "Please verify your email address before logging in. Check your inbox for the verification OTP code."
                    : "Your account has been deactivated or banned. Please contact support for assistance.",
                403);
        }

        // EF-03: Check if account is temporarily locked (brute-force protection)
        if (user.IsLocked)
        {
            await _outbox.EnqueueAsync(RoutingKeys.Auth.LoginFailed, new LoginFailedEvent
            {
                Identifier = command.Identifier,
                UserId = user.Id,
                Reason = "Account locked due to too many failed attempts",
                ErrorCode = 403
            }, cancellationToken);

            return Result<AuthResult>.Failure(
                "Your account has been locked due to too many failed login attempts. Please try again in 15 minutes or contact support.",
                403);
        }

        _logger.LogInformation(
            "User {UserId} ({Email}) logged in from IP: {IpAddress}, User-Agent: {UserAgent}",
            user.Id, user.Email, command.IpAddress ?? "Unknown", command.UserAgent ?? "Unknown");

        // Reset failed login attempts on successful login
        user.FailedLoginAttempts = 0;
        user.IsLocked = false;
        user.LockedAt = null;

        // Generate token pair
        var (accessToken, refreshToken) = _jwt.GenerateTokenPair(user);

        // P4: Extend refresh token lifetime if "Remember Me" is checked (30 days vs 7 days)
        var refreshTokenExpiry = command.RememberMe
            ? _dateTimeProvider.UtcNow.AddDays(30)
            : _dateTimeProvider.UtcNow.AddDays(7);

        // Save refresh token
        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = refreshTokenExpiry,
            CreatedAt = _dateTimeProvider.UtcNow,
            IsRevoked = false
        };

        await _refreshTokenRepository.AddAsync(refreshTokenEntity);

        // Commit transaction
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Map to AuthResult
        var authResult = user.ToAuthResult(accessToken, refreshToken);

        // Publish typed login successful event (Activity — audit trail)
        await _outbox.EnqueueAsync(RoutingKeys.Auth.LoginSuccessful, new LoginSuccessfulEvent
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email
        }, cancellationToken);

        return Result<AuthResult>.Success(authResult, "Login successful");
    }
}
