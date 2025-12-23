using System.ComponentModel.DataAnnotations;

namespace AuthService.Application.DTOs.Request
{
    public class RefreshTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; } = null!;
    }
}

