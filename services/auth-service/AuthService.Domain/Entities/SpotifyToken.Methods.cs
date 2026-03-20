using System;

namespace AuthService.Domain.Entities;

public partial class SpotifyToken
{
    public static SpotifyToken Create(Guid userId, string accessToken, string? refreshToken, int expiresInSeconds)
    {
        var now = DateTime.UtcNow;
        return new SpotifyToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = now.AddSeconds(expiresInSeconds),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateToken(string accessToken, string? refreshToken, int expiresInSeconds)
    {
        AccessToken = accessToken;
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            RefreshToken = refreshToken;
        }
        ExpiresAt = DateTime.UtcNow.AddSeconds(expiresInSeconds);
        UpdatedAt = DateTime.UtcNow;
    }
    
    public bool IsExpired()
    {
        return DateTime.UtcNow >= ExpiresAt.AddMinutes(-1); // 1 minute buffer
    }
}
