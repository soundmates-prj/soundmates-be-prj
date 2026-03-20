namespace AuthService.Application.Configuration;

/// <summary>
/// Strongly-typed configuration for the Spotify Web API integration.
///
/// Bound from the "Spotify" section in appsettings or environment variables:
///   SPOTIFY_CLIENT_ID       → Spotify:ClientId
///   SPOTIFY_CLIENT_SECRET   → Spotify:ClientSecret
///   SPOTIFY_REDIRECT_URI    → Spotify:RedirectUri
///   SPOTIFY_TOKEN_ENDPOINT  → Spotify:TokenEndpoint  (optional — defaults to standard Auth URL)
///   SPOTIFY_API_BASE        → Spotify:ApiBase         (optional — defaults to standard API URL)
///   SPOTIFY_TOKEN_BUFFER    → Spotify:TokenBufferSeconds (optional — default 60)
/// </summary>
public sealed class SpotifyOptions
{
    public const string SectionName = "Spotify";

    /// <summary>OAuth2 Client ID from developer.spotify.com dashboard.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>OAuth2 Client Secret from developer.spotify.com dashboard.</summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// OAuth redirect URI registered in Spotify Dashboard.
    /// Must use HTTPS and match exactly (scheme/host/port/path).
    /// </summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>Token endpoint. Defaults to Spotify's standard Auth URL.</summary>
    public string TokenEndpoint { get; set; } = "https://accounts.spotify.com/api/token";

    /// <summary>Base URL for the Spotify Web API. Defaults to Spotify v1.</summary>
    public string ApiBase { get; set; } = "https://api.spotify.com/v1";

    /// <summary>
    /// Seconds before token expiry to proactively refresh.
    /// Prevents 401s caused by clock skew or latency.
    /// </summary>
    public int TokenBufferSeconds { get; set; } = 60;
}
