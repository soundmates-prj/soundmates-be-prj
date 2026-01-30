using System.ComponentModel.DataAnnotations;

namespace AuthService.Api.Models.Requests
{
    public class RefreshTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; } = null!;
    }
}

