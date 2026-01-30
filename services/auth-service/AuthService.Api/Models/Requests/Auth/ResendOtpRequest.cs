using System.ComponentModel.DataAnnotations;

namespace AuthService.Api.Models.Requests;

public class ResendOtpRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = null!;
}
