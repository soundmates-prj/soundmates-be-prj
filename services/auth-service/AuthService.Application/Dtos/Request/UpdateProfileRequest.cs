using System.ComponentModel.DataAnnotations;

namespace AuthService.Application.DTOs.Request
{
    public class UpdateProfileRequest
    {
        [Required]
        [StringLength(128, MinimumLength = 1)]
        public string FirstName { get; set; } = null!;
        
        [Required]
        [StringLength(128, MinimumLength = 1)]
        public string LastName { get; set; } = null!;
    }
}

