using System;

namespace AuthService.Domain.Entities;

public partial class Profile
{
    public Guid Id { get; set; }
    
    public Guid UserId { get; set; }
    
    public string? Bio { get; set; }
    
    public string? ProfileImageUrl { get; set; }
    
    public string? BackgroundImageUrl { get; set; }
    
    public string? Phone { get; set; }
    
    public string? Gender { get; set; }
    
    public DateTime? DateOfBirth { get; set; }
    
    public string? Location { get; set; }
    
    public string? Website { get; set; }
    
    public DateTime? CreatedAt { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
    
    public virtual User User { get; set; } = null!;
}

