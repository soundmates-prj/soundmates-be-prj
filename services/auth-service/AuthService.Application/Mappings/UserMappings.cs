using AuthService.Application.Results;
using AuthService.Domain.Entities;

namespace AuthService.Application.Mappings;

/// <summary>
/// Mapping extensions for User entity
/// Separate from RoleMappings for better organization
/// </summary>
public static class UserMappings
{
    /// <summary>
    /// Map Domain User entity to Application UserResult
    /// </summary>
    public static UserResult ToResult(this User user)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));
        
        return new UserResult
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            RoleId = user.RoleId,
            RoleName = user.Role?.Name,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            EmailVerifiedAt = user.EmailVerifiedAt
        };
    }

    /// <summary>
    /// Map Domain User + tokens to Application AuthResult
    /// </summary>
    public static AuthResult ToAuthResult(this User user, string accessToken, string refreshToken)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));
        if (string.IsNullOrEmpty(accessToken)) throw new ArgumentNullException(nameof(accessToken));
        if (string.IsNullOrEmpty(refreshToken)) throw new ArgumentNullException(nameof(refreshToken));

        return new AuthResult
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            RoleId = user.RoleId,
            RoleName = user.Role?.Name,
            IsActive = user.IsActive,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            CreatedAt = user.CreatedAt
        };
    }

    /// <summary>
    /// Map Domain User + Profile to Application UserProfileResult
    /// </summary>
    public static UserProfileResult ToProfileResult(this User user, Profile? profile = null)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));

        return new UserProfileResult
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            RoleId = user.RoleId,
            RoleName = user.Role?.Name,
            IsActive = user.IsActive,
            EmailVerifiedAt = user.EmailVerifiedAt,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            
            // Profile info (nullable if profile not provided or doesn't exist)
            Bio = profile?.Bio,
            Phone = profile?.Phone,
            Gender = profile?.Gender,
            DateOfBirth = profile?.DateOfBirth,
            ProfileImageUrl = profile?.ProfileImageUrl,
            BackgroundImageUrl = profile?.BackgroundImageUrl,
            Location = profile?.Location,
            Website = profile?.Website
        };
    }
}




