using AuthService.Domain.Entities;
using AuthService.Domain.Enums;

namespace AuthService.Domain.Interfaces;

/// <summary>
/// Repository interface for OTP operations
/// </summary>
public interface IOtpRepository
{
    /// <summary>
    /// Gets OTP by email, code, and purpose if valid (not used and not expired)
    /// </summary>
    Task<OtpCode?> GetByEmailAndCodeAsync(string email, string code, OtpPurpose purpose);
    
    /// <summary>
    /// Gets the latest OTP for email and purpose
    /// </summary>
    Task<OtpCode?> GetLatestByEmailAsync(string email, OtpPurpose purpose);
    
    /// <summary>
    /// Adds a new OTP to database
    /// </summary>
    Task AddAsync(OtpCode otpCode);
    
    /// <summary>
    /// Updates existing OTP
    /// </summary>
    Task UpdateAsync(OtpCode otpCode);
    
    /// <summary>
    /// Invalidates all OTPs for given email and purpose
    /// </summary>
    Task InvalidateAllForEmailAsync(string email, OtpPurpose purpose);
}

