namespace AuthService.Domain.Enums;

/// <summary>
/// Purpose of OTP code
/// Used to distinguish between different OTP use cases
/// </summary>
public enum OtpPurpose
{
    /// <summary>
    /// OTP for password reset
    /// </summary>
    PasswordReset = 1,
    
    /// <summary>
    /// OTP for email verification
    /// </summary>
    EmailVerification = 2
}
