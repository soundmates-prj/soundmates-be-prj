using System.ComponentModel.DataAnnotations;

namespace AuthService.Api.Models.Requests.Role;

/// <summary>
/// Request model for updating an existing role
/// Role name MUST be uppercase (e.g., ADMIN, MEMBER, HOST, STAFF)
/// </summary>
public class UpdateRoleRequest : IValidatableObject
{
    [Required(ErrorMessage = "Role name is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Role name must be between 2 and 50 characters")]
    [RegularExpression(@"^[A-Z][A-Z_]*$", ErrorMessage = "Role name must be uppercase and can only contain letters and underscores (e.g., ADMIN, HOST, MEMBER)")]
    public string Name { get; set; } = null!;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Additional validation: ensure name is uppercase
        if (!string.IsNullOrEmpty(Name) && Name != Name.ToUpper())
        {
            yield return new ValidationResult(
                "Role name must be in uppercase (e.g., ADMIN, MEMBER, HOST, STAFF)",
                new[] { nameof(Name) });
        }

        // Check for valid role names (optional)
        var validRoles = new[] { "ADMIN", "MEMBER", "HOST", "STAFF", "MODERATOR", "GUEST" };
        if (!string.IsNullOrEmpty(Name) && !validRoles.Contains(Name.ToUpper()))
        {
            yield return new ValidationResult(
                $"Role name must be one of: {string.Join(", ", validRoles)}. Got: {Name}",
                new[] { nameof(Name) });
        }
    }
}

