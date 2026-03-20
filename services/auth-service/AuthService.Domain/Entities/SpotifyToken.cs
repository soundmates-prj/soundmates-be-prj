using System;

namespace AuthService.Domain.Entities;

public partial class SpotifyToken
{
    public Guid Id { get; set; }
    
    public Guid UserId { get; set; }
    
    public string AccessToken { get; set; } = null!;
    
    public string? RefreshToken { get; set; }
    
    public DateTime ExpiresAt { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
