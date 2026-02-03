using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Mappings;
using AuthService.Application.Results;
using AuthService.Application.Features.Auth.Commands;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;

namespace AuthService.Application.Features.Auth.Handlers;

public sealed class LoginHandler : ICommandHandler<LoginCommand, AuthResult>
{
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
        // Connect to repository to validate user credentials (by username or email)
        var user = await _repo.LoginByUsernameOrEmailAsync(command.Identifier, command.Password);
        if (user is null)
        {
            // Publish login failed event
            await _outbox.EnqueueAsync("auth.user.login.failed", new
            {
                identifier = command.Identifier,
                reason = "Invalid username/email or password",
                errorCode = 401,
                occurredAtUtc = _dateTimeProvider.UtcNow
            }, cancellationToken);
            
            return Result<AuthResult>.Failure("Invalid username/email or password", 401);
        }

        // Check if user has verified their email
        if (!user.IsActive)
        {
            // Publish login failed event (email not verified)
            await _outbox.EnqueueAsync("auth.user.login.failed", new
            {
                identifier = command.Identifier,
                userId = user.Id,
                reason = "Email not verified",
                errorCode = 403,
                occurredAtUtc = _dateTimeProvider.UtcNow
            }, cancellationToken);
            
            return Result<AuthResult>.Failure("Please verify your email address before logging in. Check your inbox for the verification OTP code.", 403);
        }

        // Log login activity for security monitoring
        var loginLogPayload = JsonSerializer.Serialize(new
        {
            userId = user.Id,
            email = user.Email,
            username = user.Username,
            ipAddress = command.IpAddress ?? "Unknown",
            userAgent = command.UserAgent ?? "Unknown",
            loginTime = _dateTimeProvider.UtcNow,
            isSuccessful = true
        });

        // Log to outbox for async processing (can be used for security alerts, analytics, etc.)
        await _outbox.EnqueueAsync("auth.user.login.activity", loginLogPayload, cancellationToken);

        // Log suspicious activity (different IP/location, unusual time, etc.)
        // This is a simple implementation - in production, you might want to:
        // 1. Store last login IP/location in user profile
        // 2. Compare with current login
        // 3. Use geolocation API to detect location changes
        // 4. Check for unusual login times
        _logger.LogInformation(
            "User {UserId} ({Email}) logged in from IP: {IpAddress}, User-Agent: {UserAgent}",
            user.Id, user.Email, command.IpAddress ?? "Unknown", command.UserAgent ?? "Unknown");

        // Generate token pair
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

        // Map to AuthResult
        var authResult = user.ToAuthResult(accessToken, refreshToken);

        // Publish login successful event
        await _outbox.EnqueueAsync("auth.user.login.successful", new
        {
            userId = user.Id,
            username = user.Username,
            email = user.Email,
            occurredAtUtc = _dateTimeProvider.UtcNow
        }, cancellationToken);

        return Result<AuthResult>.Success(authResult, "Login successful");
    }
}
