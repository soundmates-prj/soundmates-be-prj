using AuthService.Application.DTOs;
using AuthService.Application.DTOs.Response;
using AuthService.Application.Services.Common;
using AuthService.Application.Common;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
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
    private readonly IEmailService _emailService;
    private readonly AppSettings _appSettings;
    private readonly ILogger<RegisterHandler> _logger;
    private readonly IOtpRepository _otpRepository;
    private readonly Random _random = new Random();

    public RegisterHandler(
        IAuthRepository repo, 
        IOutbox outbox,
        IEmailService emailService,
        IOptions<AppSettings> appSettings,
        ILogger<RegisterHandler> logger,
        IOtpRepository otpRepository)
    {
        _repo = repo;
        _outbox = outbox;
        _emailService = emailService;
        _appSettings = appSettings.Value;
        _logger = logger;
        _otpRepository = otpRepository;
    }

    public async Task<ApiResponse<UserDto>> Handle(RegisterCommand command, CancellationToken cancellationToken)
    {
        try
        {
            // Validate password
            var (isValid, errorMessage) = PasswordValidator.Validate(command.Password);
            if (!isValid)
            {
                // Publish registration failed event
                await _outbox.EnqueueAsync("auth.user.registration.failed", new
                {
                    username = command.Username,
                    email = command.Email,
                    reason = errorMessage,
                    errorCode = 400,
                    occurredAtUtc = DateTime.UtcNow
                }, cancellationToken);
                
                return ApiResponse<UserDto>.FailureResponse(errorMessage!, 400);
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
                    occurredAtUtc = DateTime.UtcNow
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
                roleName = user.Role?.Name ?? "USER",
                isActive = user.IsActive,
                createdAt = user.CreatedAt
            }, cancellationToken);

            // Generate 6-digit OTP for email verification
            var otpCode = _random.Next(100000, 999999).ToString();

            // Invalidate any previous email verification OTPs for this email
            await _otpRepository.InvalidateAllForEmailAsync(user.Email, OtpPurpose.EmailVerification);

            // Create OTP entity
            var otp = new OtpCode
            {
                Id = Guid.NewGuid(),
                Email = user.Email,
                Code = otpCode,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15), // OTP expires in 15 minutes
                CreatedAt = DateTime.UtcNow,
                IsUsed = false,
                Purpose = OtpPurpose.EmailVerification
            };

            await _otpRepository.AddAsync(otp);

            // Send verification email with OTP
            var emailBody = $@"
                <html>
                <body>
                    <h2>Welcome to Soundmates!</h2>
                    <p>Hello {user.FirstName ?? user.Username},</p>
                    <p>Thank you for registering. Please use the following OTP code to verify your email address and activate your account:</p>
                    <h3 style='color: #007bff; font-size: 24px;'>{otpCode}</h3>
                    <p>This code will expire in 15 minutes.</p>
                    <p>If you didn't create this account, you can safely ignore this email.</p>
                    <p>Best regards,<br/>Soundmates Team</p>
                </body>
                </html>";

            try
            {
                await _emailService.SendEmailAsync(user.Email, "Verify your email address", emailBody);
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
                    occurredAtUtc = DateTime.UtcNow
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