using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Enums;
using AuthService.Application.Mappings;
using AuthService.Application.Results;
using AuthService.Application.Features.Auth.Commands;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AuthService.Application.Features.Auth.Handlers
{
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
            // Verify Google token
            var googleUser = await _googleAuthService.VerifyGoogleTokenAsync(command.IdToken);
            if (googleUser == null || !googleUser.EmailVerified)
            {
                // Publish Google login failed event
                await _outbox.EnqueueAsync("auth.user.google.login.failed", new
                {
                    reason = "Invalid Google token",
                    errorCode = 401,
                    occurredAtUtc = _dateTimeProvider.UtcNow
                }, cancellationToken);
                
                return Result<AuthResult>.Failure("Invalid Google token", 401);
            }

            // Check if OAuth account exists
            var oauthAccount = await _authRepository.GetOAuthAccountAsync("Google", googleUser.Id);
            User? user = null;

            if (oauthAccount != null)
            {
                // User exists, get the user
                user = await _userRepository.GetByIdAsync(oauthAccount.UserId!.Value);
            }
            else
            {
                // Check if user exists by email
                user = await _userRepository.GetByEmailAsync(googleUser.Email);
                
                if (user == null)
                {
                    // Create new user with MEMBER role as default
                    var userRole = await _roleRepository.GetByNameAsync("MEMBER");
                    if (userRole == null)
                    {
                        return Result<AuthResult>.Failure("Default MEMBER role not found", 500);
                    }

                    // Generate username from email (take part before @)
                    var usernameBase = googleUser.Email.Split('@')[0];
                    var username = usernameBase;
                    var counter = 1;
                    
                    // Ensure username is unique
                    while (await _userRepository.GetByUsernameAsync(username) != null)
                    {
                        username = $"{usernameBase}{counter}";
                        counter++;
                    }

                    // Split Google name into first and last name
                    var nameParts = googleUser.Name?.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
                    var firstName = nameParts.Length > 0 ? nameParts[0] : string.Empty;
                    var lastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;

                    user = new User
                    {
                        Id = Guid.NewGuid(),
                        Username = username,
                        Email = googleUser.Email,
                        FirstName = firstName,
                        LastName = lastName,
                        Password = string.Empty, // No password for OAuth users
                        RoleId = userRole.Id,
                        IsActive = true, // Google users are automatically active (email already verified by Google)
                        EmailVerifiedAt = _dateTimeProvider.UtcNow, // Mark as verified since Google verified it
                        CreatedAt = _dateTimeProvider.UtcNow
                    };

                    await _userRepository.AddAsync(user);

                    // Publish auth.user.created event for new user
                    await _outbox.EnqueueAsync("auth.user.created", new
                    {
                        id = user.Id,
                        username = user.Username,
                        email = user.Email,
                        firstName = user.FirstName,
                        lastName = user.LastName,
                        roleId = user.RoleId,
                        roleName = userRole.Name,
                        isActive = user.IsActive,
                        createdAt = user.CreatedAt
                    }, cancellationToken);
                }

                // Create or update OAuth account
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
                // Publish Google login failed event
                await _outbox.EnqueueAsync("auth.user.google.login.failed", new
                {
                    reason = "Failed to retrieve or create user",
                    errorCode = 500,
                    occurredAtUtc = _dateTimeProvider.UtcNow
                }, cancellationToken);
                
                return Result<AuthResult>.Failure("Failed to retrieve or create user", 500);
            }

            // Load user with role
            user = await _userRepository.GetByIdAsync(user.Id);
            if (user == null)
            {
                return Result<AuthResult>.Failure("User not found", 404);
            }

            // Generate token pair
            var (accessToken, refreshToken) = _jwtTokenGenerator.GenerateTokenPair(user);

            // Save refresh token
            var refreshTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = refreshToken,
                ExpiresAt = _dateTimeProvider.UtcNow.AddDays(7), // Refresh token expires in 7 days
                CreatedAt = _dateTimeProvider.UtcNow,
                IsRevoked = false
            };

            await _refreshTokenRepository.AddAsync(refreshTokenEntity);
            
            // Commit transaction
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Map to AuthResult
            var authResult = user.ToAuthResult(accessToken, refreshToken);

            // Publish event
            await _outbox.EnqueueAsync("auth.user.google.login.successful", new
            {
                user.Id,
                user.Email,
                user.Username,
                occurredAtUtc = DateTime.UtcNow
            }, cancellationToken);

            return Result<AuthResult>.Success(authResult, "Google login successful");
        }
    }
}

