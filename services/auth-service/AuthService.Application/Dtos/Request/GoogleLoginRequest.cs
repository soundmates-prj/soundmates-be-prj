using System.ComponentModel.DataAnnotations;

namespace AuthService.Application.DTOs.Request
{
    public class GoogleLoginRequest
    {
        [Required]
        public string IdToken { get; set; } = null!;
    }
}

