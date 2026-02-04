using AuthService.Application.Features.Common;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AuthService.Infrastructure.Services;

public sealed class OtpService : IOtpService
{
    private readonly IOtpRepository _otpRepository;
    private readonly IEmailService _emailService;
    private readonly ILogger<OtpService> _logger;
    private readonly Random _random = new();

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
        var otpCode = _random.Next(100000, 999999).ToString();

        await _otpRepository.InvalidateAllForEmailAsync(email, purpose);

        var otp = new OtpCode
        {
            Id = Guid.NewGuid(),
            Email = email,
            Code = otpCode,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            CreatedAt = DateTime.UtcNow,
            IsUsed = false,
            Purpose = purpose
        };

        await _otpRepository.AddAsync(otp);

        var (subject, emailBody) =
            BuildEmailContent(otpCode, userName, firstName, purpose);

        try
        {
            await _emailService.SendEmailAsync(email, subject, emailBody);
            _logger.LogInformation(
                "OTP sent successfully to {Email} for purpose {Purpose}",
                email,
                purpose);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send OTP email to {Email} for purpose {Purpose}",
                email,
                purpose);
            throw;
        }

        return otpCode;
    }

    // ===================== PRIVATE =====================

    private static (string Subject, string Body) BuildEmailContent(
        string otpCode,
        string userName,
        string? firstName,
        OtpPurpose purpose)
    {
        string baseTemplate(
            string title,
            string intro,
            string actionText)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
<meta charset='UTF-8'>
<title>{title}</title>
<style>
body {{
    margin: 0;
    padding: 0;
    background-color: #f3f4f6;
    font-family: Arial, Helvetica, sans-serif;
}}

.container {{
    max-width: 600px;
    margin: 40px auto;
    background-color: #ffffff;
    border-radius: 12px;
    overflow: hidden;
    box-shadow: 0 10px 30px rgba(0,0,0,0.08);
}}

.header {{
    background: linear-gradient(135deg, #6366f1, #8b5cf6);
    color: #ffffff;
    padding: 24px;
    text-align: center;
}}

.header h1 {{
    margin: 0;
    font-size: 26px;
}}

.content {{
    padding: 32px;
    color: #374151;
}}

.content p {{
    font-size: 15px;
    line-height: 1.6;
}}

.otp-box {{
    margin: 24px auto;
    text-align: center;
    background: #f9fafb;
    border-radius: 10px;
    padding: 20px;
    border: 1px dashed #6366f1;
}}

.otp-code {{
    font-size: 32px;
    letter-spacing: 6px;
    font-weight: bold;
    color: #6366f1;
}}

.note {{
    font-size: 13px;
    color: #6b7280;
}}

.footer {{
    background-color: #f9fafb;
    padding: 20px;
    text-align: center;
    font-size: 12px;
    color: #9ca3af;
}}
</style>
</head>

<body>
<div class='container'>

    <div class='header'>
        <h1>SoundMates</h1>
    </div>

    <div class='content'>
        <p>Xin chào {userName},</p>

        <p>{intro}</p>

        <div class='otp-box'>
            <div class='otp-code'>{otpCode}</div>
        </div>

        <p>{actionText}</p>

        <p class='note'>
            Mã OTP có hiệu lực trong <b>15 phút</b>.
        </p>

        <p class='note'>
            Nếu bạn không thực hiện yêu cầu này, hãy bỏ qua email.
        </p>
    </div>

    <div class='footer'>
        © {DateTime.UtcNow.Year} SoundMates. All rights reserved.
    </div>

</div>
</body>
</html>";
        }

        return purpose switch
        {
            OtpPurpose.EmailVerification => (
                Subject: "Xác minh email của bạn",
                Body: baseTemplate(
                    "Xác minh email",
                    "Cảm ơn bạn đã đăng ký SoundMates. Vui lòng sử dụng mã OTP bên dưới để xác minh email của bạn.",
                    "Nhập mã này để kích hoạt tài khoản."
                )
            ),

            OtpPurpose.PasswordReset => (
                Subject: "Đặt lại mật khẩu",
                Body: baseTemplate(
                    "Đặt lại mật khẩu",
                    "Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn.",
                    "Nhập mã này để tiếp tục đặt lại mật khẩu."
                )
            ),

            _ => throw new ArgumentException($"Unsupported OTP purpose: {purpose}")
        };
    }
}
