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
        var user = await _repo.LoginByUsernameOrEmailAsync(command.Identifier, command.Password);
        if (user is null)
        {
            await _outbox.EnqueueAsync(RoutingKeys.Auth.LoginFailed, new LoginFailedEvent
            {
                Identifier = command.Identifier,
                Reason = "Invalid username/email or password",
                ErrorCode = 401
            }, cancellationToken);

            return Result<AuthResult>.Failure("Invalid username/email or password", 401);
        }

        if (!user.IsActive)
        {
            await _outbox.EnqueueAsync(RoutingKeys.Auth.LoginFailed, new LoginFailedEvent
            {
                Identifier = command.Identifier,
                UserId = user.Id,
                Reason = "Email not verified",
                ErrorCode = 403
            }, cancellationToken);

            return Result<AuthResult>.Failure(
                "Please verify your email address before logging in. Check your inbox for the verification OTP code.",
                403);
        }

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
