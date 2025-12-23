using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.DTOs.Response;
using AuthService.Application.Services.Auth.Commands;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AuthService.Application.Services.Auth.Handlers
{
    public sealed class ForgetPasswordRequestHandler : ICommandHandler<ForgetPasswordRequestCommand, bool>
    {
        private readonly IUserRepository _userRepository;
        private readonly IOtpRepository _otpRepository;
        private readonly IEmailService _emailService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly Random _random = new Random();

        public ForgetPasswordRequestHandler(
            IUserRepository userRepository,
            IOtpRepository otpRepository,
            IEmailService emailService,
            IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _otpRepository = otpRepository;
            _emailService = emailService;
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<bool>> Handle(ForgetPasswordRequestCommand command, CancellationToken cancellationToken)
        {
            // Check if user exists
            var user = await _userRepository.GetByEmailAsync(command.Email);
            if (user == null)
            {
                // Don't reveal if email exists for security
                return ApiResponse<bool>.SuccessResponse(true, "If the email exists, an OTP code has been sent");
            }

            // Invalidate previous OTPs for this email
            await _otpRepository.InvalidateAllForEmailAsync(command.Email, OtpPurpose.PasswordReset);

            // Generate 6-digit OTP
            var otpCode = _random.Next(100000, 999999).ToString();

            // Create OTP entity
            var otp = new OtpCode
            {
                Id = Guid.NewGuid(),
                Email = command.Email,
                Code = otpCode,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10), // OTP expires in 10 minutes
                CreatedAt = DateTime.UtcNow,
                IsUsed = false,
                Purpose = OtpPurpose.PasswordReset
            };

            await _otpRepository.AddAsync(otp);

            // Send email with OTP
            var subject = "Password Reset OTP Code";
            var body = $@"
                <html>
                <body>
                    <h2>Password Reset Request</h2>
                    <p>Hello {(!string.IsNullOrEmpty(user.FirstName) ? $"{user.FirstName} {user.LastName}".Trim() : user.Username)},</p>
                    <p>You have requested to reset your password. Please use the following OTP code:</p>
                    <h3 style='color: #007bff; font-size: 24px;'>{otpCode}</h3>
                    <p>This code will expire in 10 minutes.</p>
                    <p>If you did not request this, please ignore this email.</p>
                    <br>
                    <p>Best regards,<br>EduLit Team</p>
                </body>
                </html>";

            try
            {
                await _emailService.SendEmailAsync(command.Email, subject, body);
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.FailureResponse($"Failed to send email: {ex.Message}", 500);
            }

            return ApiResponse<bool>.SuccessResponse(true, "OTP code has been sent to your email");
        }
    }
}

