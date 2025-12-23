using System.ComponentModel.DataAnnotations;

namespace AuthService.Application.DTOs.Request
{
    public class ForgetPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;
    }
}

