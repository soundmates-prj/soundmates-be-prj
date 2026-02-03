using AuthService.Domain.Enums;

namespace AuthService.Application.Features.Common;

/// <summary>
/// Service interface for OTP operations
/// </summary>
public interface IOtpService
{
    /// <summary>
    /// Generates and sends OTP code via email
    /// </summary>
    /// <param name="email">Recipient email address</param>
    /// <param name="userName">User's username</param>
    /// <param name="firstName">User's first name (optional)</param>
    /// <param name="purpose">Purpose of OTP (EmailVerification or PasswordReset)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated OTP code</returns>
    Task<string> GenerateAndSendOtpAsync(
        string email,
        string userName,
        string? firstName,
        OtpPurpose purpose,
        CancellationToken cancellationToken = default);
}
