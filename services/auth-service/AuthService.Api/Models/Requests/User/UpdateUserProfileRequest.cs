namespace AuthService.Api.Models.Requests.User;

/// <summary>
/// Request model for updating user profile (API Layer)
/// All fields are optional - only provided fields will be updated
/// </summary>
public class UpdateUserProfileRequest
{
    // Basic user info
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    
    // Extended profile fields
    public string? Bio { get; set; }
    public string? Phone { get; set; }
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string? BackgroundImageUrl { get; set; }
    public string? Location { get; set; }
    public string? Website { get; set; }
}

