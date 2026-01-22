using System.ComponentModel.DataAnnotations;

namespace AuthService.Application.DTOs.Request;

public class ResendOtpRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = null!;
}
