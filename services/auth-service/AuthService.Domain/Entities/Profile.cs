namespace AuthService.Domain.Entities;

/// <summary>
/// User profile entity
/// Extended information about a user
/// </summary>
public partial class Profile
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Foreign key to User
    /// </summary>
    public Guid UserId { get; set; }
    
    /// <summary>
    /// User biography/description
    /// </summary>
    public string? Bio { get; set; }
    
    /// <summary>
    /// URL to user's profile image
    /// </summary>
    public string? ProfileImageUrl { get; set; }
    
    /// <summary>
    /// URL to user's background/cover image
    /// </summary>
    public string? BackgroundImageUrl { get; set; }
    
    /// <summary>
    /// User's phone number
    /// </summary>
    public string? Phone { get; set; }
    
    /// <summary>
    /// User's gender (stored as string: "Male" or "Female")
    /// Use Gender enum for validation: Gender.Male, Gender.Female
    /// </summary>
    public string? Gender { get; set; }
    
    /// <summary>
    /// User's date of birth
    /// </summary>
    public DateTime? DateOfBirth { get; set; }
    
    /// <summary>
    /// User's location/city
    /// </summary>
    public string? Location { get; set; }
    
    /// <summary>
    /// User's website URL
    /// </summary>
    public string? Website { get; set; }
    
    /// <summary>
    /// When profile was created
    /// </summary>
    public DateTime? CreatedAt { get; set; }
    
    /// <summary>
    /// When profile was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// Navigation property to User
    /// </summary>
    public virtual User User { get; set; } = null!;
}

