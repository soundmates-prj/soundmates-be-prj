using AuthService.Domain.Enums;

namespace AuthService.Domain.Entities;

/// <summary>
/// OTP (One-Time Password) code entity
/// Used for email verification and password reset flows
/// </summary>
public class OtpCode
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Email address this OTP is sent to
    /// </summary>
    public string Email { get; set; } = null!;
    
    /// <summary>
    /// 6-digit OTP code
    /// </summary>
    public string Code { get; set; } = null!;
    
    /// <summary>
    /// When this OTP expires (typically 15 minutes from creation)
    /// </summary>
    public DateTime ExpiresAt { get; set; }
    
    /// <summary>
    /// When this OTP was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Whether this OTP has been used
    /// </summary>
    public bool IsUsed { get; set; }
    
    /// <summary>
    /// When this OTP was used (if used)
    /// </summary>
    public DateTime? UsedAt { get; set; }
    
    /// <summary>
    /// Purpose of this OTP (EmailVerification or PasswordReset)
    /// </summary>
    public OtpPurpose Purpose { get; set; }
}

