using System;

namespace AuthService.Domain.Entities
{
    public class OtpCode
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = null!;
        public string Code { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsUsed { get; set; }
        public DateTime? UsedAt { get; set; }
        public OtpPurpose Purpose { get; set; }
    }

    public enum OtpPurpose
    {
        PasswordReset = 1,
        EmailVerification = 2
    }
}

