namespace AuthService.Application.Results;

/// <summary>
/// User profile result containing user info + extended profile data
/// Used for profile operations
/// </summary>
public sealed class UserProfileResult
{
    // User basic info
    public Guid Id { get; init; }
    public string Username { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public Guid? RoleId { get; init; }
    public string? RoleName { get; init; }
    public bool IsActive { get; init; }
    public DateTime? EmailVerifiedAt { get; init; }
    public DateTime? CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    
    // Extended profile info
    public string? Bio { get; init; }
    public string? Phone { get; init; }
    public string? Gender { get; init; }
    public DateTime? DateOfBirth { get; init; }
    public string? ProfileImageUrl { get; init; }
    public string? BackgroundImageUrl { get; init; }
    public string? Location { get; init; }
    public string? Website { get; init; }
}
