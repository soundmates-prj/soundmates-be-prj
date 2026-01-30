using System.ComponentModel.DataAnnotations;

namespace AuthService.Api.Models.Requests;

public class VerifyEmailRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "OTP code is required")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP code must be 6 digits")]
    public string OtpCode { get; set; } = null!;
}

