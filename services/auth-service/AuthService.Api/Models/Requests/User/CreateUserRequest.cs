using System.ComponentModel.DataAnnotations;

namespace AuthService.Api.Models.Requests.User;

/// <summary>
/// Request model for creating a new user (Admin only)
/// </summary>
/// <remarks>
/// Important notes:
/// - If RoleId is not provided, the user will be assigned the default MEMBER role
/// - Password must be at least 6 characters
/// - This endpoint is for admin use only (manual user creation)
/// - For normal user registration, use POST /api/v1/auth/register instead
/// </remarks>
public class CreateUserRequest
{
    [Required(ErrorMessage = "Username is required")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
    public string Username { get; set; } = null!;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
    public string Password { get; set; } = null!;

    [StringLength(50)]
    public string? FirstName { get; set; }

    [StringLength(50)]
    public string? LastName { get; set; }

    /// <summary>
    /// Role ID to assign to the user. If null, MEMBER role will be assigned by default.
    /// Valid roles: ADMIN, MEMBER, HOST, STAFF
    /// </summary>
    public Guid? RoleId { get; set; }
}

