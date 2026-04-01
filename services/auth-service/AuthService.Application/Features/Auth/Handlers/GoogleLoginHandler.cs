using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Mappings;
using AuthService.Application.Results;
using AuthService.Application.Features.Auth.Commands;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Shared.Contracts;
using Shared.Contracts.Events.Auth;
using Shared.Contracts.Events.Activity;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AuthService.Application.Features.Auth.Handlers;

public sealed class GoogleLoginHandler : ICommandHandler<GoogleLoginCommand, AuthResult>
{
    private readonly IGoogleAuthService _googleAuthService;
    private readonly IAuthRepository _authRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IOutboxRepository _outbox;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public GoogleLoginHandler(
        IGoogleAuthService googleAuthService,
        IAuthRepository authRepository,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenRepository refreshTokenRepository,
        IOutboxRepository outbox,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _googleAuthService = googleAuthService;
        _authRepository = authRepository;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenRepository = refreshTokenRepository;
        _outbox = outbox;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AuthResult>> Handle(GoogleLoginCommand command, CancellationToken cancellationToken)
    {
        var googleUser = await _googleAuthService.VerifyGoogleTokenAsync(command.IdToken);
        if (googleUser == null || !googleUser.EmailVerified)
        {
            await _outbox.EnqueueAsync(RoutingKeys.Auth.GoogleLoginFailed, new GoogleLoginFailedEvent
            {
                Reason = "Invalid Google token",
                ErrorCode = 401
            }, cancellationToken);

            return Result<AuthResult>.Failure("Invalid Google token", 401);
        }

        var oauthAccount = await _authRepository.GetOAuthAccountAsync("Google", googleUser.Id);
        User? user = null;

        if (oauthAccount != null)
        {
            user = await _userRepository.GetByIdAsync(oauthAccount.UserId!.Value);
        }
        else
        {
            user = await _userRepository.GetByEmailAsync(googleUser.Email);

            if (user == null)
            {
                var userRole = await _roleRepository.GetByNameAsync("MEMBER");
                if (userRole == null)
                {
                    return Result<AuthResult>.Failure("Default MEMBER role not found", 500);
                }

                var usernameBase = googleUser.Email.Split('@')[0];
                var username = usernameBase;
                var counter = 1;
                while (await _userRepository.GetByUsernameAsync(username) != null)
                {
                    username = $"{usernameBase}{counter}";
                    counter++;
                }

                var nameParts = googleUser.Name?.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries) ?? [];
                var firstName = nameParts.Length > 0 ? nameParts[0] : string.Empty;
                var lastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;

                user = new User
                {
                    Id = Guid.NewGuid(),
                    Username = username,
                    Email = googleUser.Email,
                    FirstName = firstName,
                    LastName = lastName,
                    Password = string.Empty,
                    RoleId = userRole.Id,
                    IsActive = true,
                    EmailVerifiedAt = _dateTimeProvider.UtcNow,
                    CreatedAt = _dateTimeProvider.UtcNow
                };

                await _userRepository.AddAsync(user);

                // Publish typed UserCreatedEvent (Auth — domain state change)
                var evt = new UserCreatedEvent
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FirstName = user.FirstName ?? string.Empty,
                    LastName = user.LastName ?? string.Empty,
                    RoleId = user.RoleId ?? Guid.Empty,
                    RoleName = userRole.Name,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt ?? DateTime.UtcNow
                };
                await _outbox.EnqueueAsync(RoutingKeys.Auth.UserCreated, evt, cancellationToken);
            }

            var existingOAuth = await _authRepository.GetOAuthAccountAsync("Google", googleUser.Id);
            if (existingOAuth == null)
            {
                var oauth = new Oauthaccount
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Provider = "Google",
                    ProviderAccountId = googleUser.Id
                };
                await _authRepository.AddOAuthAccountAsync(oauth);
            }
        }

        if (user == null)
        {
            await _outbox.EnqueueAsync(RoutingKeys.Auth.GoogleLoginFailed, new GoogleLoginFailedEvent
            {
                Reason = "Failed to retrieve or create user",
                ErrorCode = 500
            }, cancellationToken);

            return Result<AuthResult>.Failure("Failed to retrieve or create user", 500);
        }

        user = await _userRepository.GetByIdAsync(user.Id);
        if (user == null)
            return Result<AuthResult>.Failure("User not found", 404);

        var (accessToken, refreshToken) = _jwtTokenGenerator.GenerateTokenPair(user);

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
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var authResult = user.ToAuthResult(accessToken, refreshToken);

        // Publish typed Google login successful event (Activity — audit trail)
        await _outbox.EnqueueAsync(RoutingKeys.Auth.GoogleLoginSuccessful, new GoogleLoginSuccessfulEvent
        {
            UserId = user.Id,
            Email = user.Email,
            Username = user.Username
        }, cancellationToken);

        return Result<AuthResult>.Success(authResult, "Google login successfully!");
    }
}