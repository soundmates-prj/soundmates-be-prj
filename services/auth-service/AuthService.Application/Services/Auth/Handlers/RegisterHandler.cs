using AuthService.Application.DTOs;
using AuthService.Application.DTOs.Response;
using AuthService.Application.Services.Common;
using AuthService.Application.Configuration;
using AuthService.Domain.Entities;
using AuthService.Domain.Exceptions;
using AuthService.Domain.Interfaces;
using AuthService.Domain.Rules;
using AuthService.Application.Abstractions.Messaging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AuthService.Application.Services.Auth.Handlers;

public sealed class RegisterHandler : ICommandHandler<RegisterCommand, UserDto>
{
    private readonly IAuthRepository _repo;
    private readonly IOutbox _outbox;
    private readonly AppSettings _appSettings;
    private readonly ILogger<RegisterHandler> _logger;
    private readonly IOtpService _otpService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RegisterHandler(
        IAuthRepository repo, 
        IOutbox outbox,
        IOptions<AppSettings> appSettings,
        ILogger<RegisterHandler> logger,
        IOtpService otpService,
        IDateTimeProvider dateTimeProvider)
    {
        _repo = repo;
        _outbox = outbox;
        _appSettings = appSettings.Value;
        _logger = logger;
        _otpService = otpService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApiResponse<UserDto>> Handle(RegisterCommand command, CancellationToken cancellationToken)
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
                
                return ApiResponse<UserDto>.FailureResponse(ex.Message, ex.StatusCode);
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
                
                return ApiResponse<UserDto>.FailureResponse("Registration failed", 400);
            }

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

            var dto = new UserDto(user);

            return ApiResponse<UserDto>.SuccessResponse(dto, "Registration successful. Please check your email to verify your account.");
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