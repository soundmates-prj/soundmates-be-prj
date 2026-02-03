using AuthService.Application.Results;
using AuthService.Domain.Entities;

namespace AuthService.Application.Mappings;

/// <summary>
/// Mapping extensions for Role entity
/// Separate from UserMappings for better organization
/// </summary>
public static class RoleMappings
{
    /// <summary>
    /// Map Domain UserRole entity to Application RoleResult
    /// </summary>
    public static RoleResult ToResult(this UserRole role)
    {
        if (role == null) throw new ArgumentNullException(nameof(role));

        return new RoleResult
        {
            Id = role.Id,
            Name = role.Name
        };
    }
}
