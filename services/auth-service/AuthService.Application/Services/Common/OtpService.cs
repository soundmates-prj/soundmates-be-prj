using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Services.Common;

public class OtpService : IOtpService
{
    private readonly IOtpRepository _otpRepository;
    private readonly IEmailService _emailService;
    private readonly ILogger<OtpService> _logger;
    private readonly Random _random = new Random();

    public OtpService(
        IOtpRepository otpRepository,
        IEmailService emailService,
        ILogger<OtpService> logger)
    {
        _otpRepository = otpRepository;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<string> GenerateAndSendOtpAsync(
        string email,
        string userName,
        string? firstName,
        OtpPurpose purpose,
        CancellationToken cancellationToken = default)
    {
        // Generate 6-digit OTP
        var otpCode = _random.Next(100000, 999999).ToString();

        // Invalidate any previous OTPs for this email and purpose
        await _otpRepository.InvalidateAllForEmailAsync(email, purpose);

        // Create new OTP entity
        var otp = new OtpCode
        {
            Id = Guid.NewGuid(),
            Email = email,
            Code = otpCode,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15), // OTP expires in 15 minutes
            CreatedAt = DateTime.UtcNow,
            IsUsed = false,
            Purpose = purpose
        };

        await _otpRepository.AddAsync(otp);

        // Send email based on purpose
        var (subject, emailBody) = BuildEmailContent(otpCode, userName, firstName, purpose);

        try
        {
            await _emailService.SendEmailAsync(email, subject, emailBody);
            _logger.LogInformation("OTP sent successfully to {Email} for purpose {Purpose}", email, purpose);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send OTP email to {Email} for purpose {Purpose}", email, purpose);
            throw; // Re-throw to let caller handle the email failure
        }

        return otpCode;
    }

    private (string Subject, string Body) BuildEmailContent(
        string otpCode,
        string userName,
        string? firstName,
        OtpPurpose purpose)
    {
        var displayName = firstName ?? userName;

        return purpose switch
        {
            OtpPurpose.EmailVerification => (
                Subject: "Verify your email address",
                Body: $@"
                    <html>
                    <body>
                        <h2>Welcome to Soundmates!</h2>
                        <p>Hello {displayName},</p>
                        <p>Thank you for registering. Please use the following OTP code to verify your email address and activate your account:</p>
                        <h3 style='color: #007bff; font-size: 24px;'>{otpCode}</h3>
                        <p>This code will expire in 15 minutes.</p>
                        <p>If you didn't create this account, you can safely ignore this email.</p>
                        <p>Best regards,<br/>Soundmates Team</p>
                    </body>
                    </html>"
            ),

            OtpPurpose.PasswordReset => (
                Subject: "Reset your password",
                Body: $@"
                    <html>
                    <body>
                        <h2>Password Reset - Soundmates</h2>
                        <p>Hello {displayName},</p>
                        <p>You have requested to reset your password. Please use the following OTP code:</p>
                        <h3 style='color: #007bff; font-size: 24px;'>{otpCode}</h3>
                        <p>This code will expire in 15 minutes.</p>
                        <p>If you didn't request this password reset, please ignore this email and your password will remain unchanged.</p>
                        <p>Best regards,<br/>Soundmates Team</p>
                    </body>
                    </html>"
            ),

            _ => throw new ArgumentException($"Unsupported OTP purpose: {purpose}")
        };
    }
}
