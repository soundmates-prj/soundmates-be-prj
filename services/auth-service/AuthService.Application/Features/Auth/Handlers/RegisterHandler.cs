using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Configuration;
using AuthService.Application.Mappings;
using AuthService.Application.Results;
using AuthService.Application.Features.Auth.Commands;
using AuthService.Application.Features.Common;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Domain.Exceptions;
using AuthService.Domain.Interfaces;
using AuthService.Domain.Rules;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AuthService.Application.Features.Auth.Handlers;

public sealed class RegisterHandler : ICommandHandler<RegisterCommand, AuthResult>
{
    private readonly IAuthRepository _repo;
    private readonly IOutboxRepository _outbox;
    private readonly AppSettings _appSettings;
    private readonly ILogger<RegisterHandler> _logger;
    private readonly IOtpService _otpService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenGenerator _jwt;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public RegisterHandler(
        IAuthRepository repo, 
        IOutboxRepository outbox,
        IOptions<AppSettings> appSettings,
        ILogger<RegisterHandler> logger,
        IOtpService otpService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork,
        IJwtTokenGenerator jwt,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _repo = repo;
        _outbox = outbox;
        _appSettings = appSettings.Value;
        _logger = logger;
        _otpService = otpService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
        _jwt = jwt;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<Result<AuthResult>> Handle(RegisterCommand command, CancellationToken cancellationToken)
    {
        try
        {
            // Validate password using Domain Rule
            try
            {
                PasswordRule.Validate(command.Password);
            }
            catch (UserValidationException ex)
            {
                // Publish registration failed event
                await _outbox.EnqueueAsync("auth.user.registration.failed", new
                {
                    username = command.Username,
                    email = command.Email,
                    reason = ex.Message,
                    errorCode = ex.ErrorCode,
                    occurredAtUtc = _dateTimeProvider.UtcNow
                }, cancellationToken);
                
                return Result<AuthResult>.Failure(ex.Message, ex.StatusCode);
            }

            var user = await _repo.RegisterAsync(command.Username, command.Email, command.Password, command.FirstName, command.LastName);
            if (user is null)
            {
                // Publish registration failed event
                await _outbox.EnqueueAsync("auth.user.registration.failed", new
                {
                    username = command.Username,
                    email = command.Email,
                    reason = "Registration failed",
                    errorCode = 400,
                    occurredAtUtc = _dateTimeProvider.UtcNow
                }, cancellationToken);
                
                return Result<AuthResult>.Failure("Registration failed", 400);
            }
            
            // Commit transaction
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Ensure Role is loaded for token generation and DTO mapping
            if (user.Role == null && user.RoleId.HasValue)
            {
                // Role should be loaded by RegisterAsync, but if not, we need to reload
                // This shouldn't happen, but adding as safety check
                throw new InvalidOperationException($"User role not loaded for user {user.Id}");
            }

            // Publish event - use "auth.user.created" to match consumer expectations
            await _outbox.EnqueueAsync("auth.user.created", new
            {
                id = user.Id,
                username = user.Username,
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName,
                roleId = user.RoleId,
                // Should always be loaded; fallback kept as safety but aligned to default MEMBER
                roleName = user.Role?.Name ?? "MEMBER",
                isActive = user.IsActive,
                createdAt = user.CreatedAt
            }, cancellationToken);

            // Use shared OTP service to generate and send verification email
            try
            {
                await _otpService.GenerateAndSendOtpAsync(
                    user.Email,
                    user.Username,
                    user.FirstName,
                    OtpPurpose.EmailVerification,
                    cancellationToken);
                
                _logger.LogInformation("Verification OTP sent successfully to {Email}", user.Email);
            }
            catch (Exception ex)
            {
                // Log email sending failure but don't fail registration
                // User can request resend verification email later
                _logger.LogError(ex, "Failed to send verification email to {Email}. User can verify later.", user.Email);
            }

            // Generate tokens for immediate login after registration
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
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            // Map to AuthResult
            var authResult = user.ToAuthResult(accessToken, refreshToken);

            return Result<AuthResult>.Success(authResult, "Registration successful. Please check your email to verify your account.");
        }
        catch (Application.Exceptions.AuthException ex)
        {
            // Publish registration failed event
            try
            {
                await _outbox.EnqueueAsync("auth.user.registration.failed", new
                {
                    username = command.Username,
                    email = command.Email,
                    reason = ex.Message,
                    errorCode = (int)ex.ErrorCode,
                    occurredAtUtc = _dateTimeProvider.UtcNow
                }, cancellationToken);
            }
            catch
            {
                // Ignore outbox errors when re-throwing
            }
            
            // Re-throw AuthException so it can be handled by the controller
            throw;
        }
        catch (Exception ex)
        {
            // Publish registration failed event for unexpected errors
            try
            {
                await _outbox.EnqueueAsync("auth.user.registration.failed", new
                {
                    username = command.Username,
                    email = command.Email,
                    error = "Unhandled error occurred during registration",
                    detail = ex.Message,
                    occurredAtUtc = DateTime.UtcNow
                }, cancellationToken);
            }
            catch
            {
                // Ignore outbox errors when re-throwing
            }
            
            // Log and wrap unexpected exceptions
            throw new InvalidOperationException($"Registration failed: {ex.Message}", ex);
        }
    }
}
