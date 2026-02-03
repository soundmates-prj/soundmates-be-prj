using System.ComponentModel.DataAnnotations;

namespace AuthService.Api.Models.Requests
{
    public class ForgetPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;
    }
}

