using System.ComponentModel.DataAnnotations;

namespace AuthService.Application.DTOs.Request
{
    public class ResetPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        [StringLength(10, MinimumLength = 6)]
        public string OtpCode { get; set; } = null!;

        [Required]
        [MinLength(8)]
        public string NewPassword { get; set; } = null!;
    }
}

