using System.ComponentModel.DataAnnotations;

namespace AuthService.Api.Models.Requests
{
    public class ChangePasswordRequest
    {
        [Required]
        public string OldPassword { get; set; } = null!;

        [Required]
        [MinLength(8)]
        public string NewPassword { get; set; } = null!;
    }
}

