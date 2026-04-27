using System.ComponentModel.DataAnnotations;

namespace AuthService.Api.Models.Requests.User;

/// <summary>
/// Request model for updating user info (Admin only)
/// </summary>
public class UpdateUserRequest
{
    [Required(ErrorMessage = "Username is required")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
    public string Username { get; set; } = null!;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = null!;

    [StringLength(50)]
    public string? FirstName { get; set; }

    [StringLength(50)]
    public string? LastName { get; set; }

    public Guid? RoleId { get; set; }
}
