using System;
using System.Collections.Generic;

namespace AuthService.Domain.Entities;

public partial class User
{
    public Guid Id { get; set; }

    public Guid? RoleId { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsActive { get; set; } = false;

    public string? EmailVerificationToken { get; set; }

    public DateTime? EmailVerifiedAt { get; set; }

    public virtual ICollection<Oauthaccount> Oauthaccounts { get; set; } = new List<Oauthaccount>();

    public virtual UserRole? Role { get; set; }
    
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    
    public virtual Profile? Profile { get; set; }

    public virtual ICollection<UserFavourite> UserFavourites { get; set; } = new List<UserFavourite>();
}
