using System.ComponentModel.DataAnnotations;

namespace AuthService.Api.Models.Requests
{
    public class GoogleLoginRequest
    {
        [Required]
        public string IdToken { get; set; } = null!;
    }
}

