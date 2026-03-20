using System.Threading;
using System.Threading.Tasks;

namespace AuthService.Application.Abstractions;

public interface ISpotifyUserApiClient
{
    Task<SpotifyOAuthTokenResponse?> ExchangeCodeAsync(string code, string redirectUri, CancellationToken ct = default);
    Task<SpotifyOAuthTokenResponse?> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task<SpotifyUserProfile?> GetUserProfileAsync(string accessToken, CancellationToken ct = default);
}

public record SpotifyOAuthTokenResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    string? RefreshToken,
    string? Scope);

public record SpotifyUserProfile(
    string Id,
    string DisplayName,
    string Email,
    string? Country,
    string? Product,
    string? Uri,
    string? ImageUrl);
