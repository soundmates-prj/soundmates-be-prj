namespace AuthService.Application.Abstractions;

/// <summary>
/// Spotify Web API client for auth-service (write side).
/// Used to enrich favourite metadata at the moment of creation:
/// when a user adds a Spotify item, we fetch the real name, artist,
/// album art, preview URL, etc. from Spotify rather than trusting
/// whatever the client sends.
/// </summary>
public interface ISpotifyApiClient
{
    /// <summary>Get a track's metadata by Spotify track ID.</summary>
    Task<SpotifyTrackInfo?> GetTrackAsync(string trackId, CancellationToken ct = default);

    /// <summary>Get an artist's metadata by Spotify artist ID.</summary>
    Task<SpotifyArtistInfo?> GetArtistAsync(string artistId, CancellationToken ct = default);

    /// <summary>Get an album's metadata by Spotify album ID.</summary>
    Task<SpotifyAlbumInfo?> GetAlbumAsync(string albumId, CancellationToken ct = default);
}

// ── Slim DTOs for auth-service (only what we store) ───────────────────────────
// Deliberately thin — we only need what goes into user_favourites_read.
// The full Spotify DTO lives in auth-query-service for the search proxy.

public record SpotifyTrackInfo(
    string Id,
    string Name,
    string ArtistName,
    string AlbumName,
    string? ImgUrl,
    string? PreviewUrl,
    int DurationMs,
    string? ExternalUrl);

public record SpotifyArtistInfo(
    string Id,
    string Name,
    string? ImgUrl,
    string? ExternalUrl);

public record SpotifyAlbumInfo(
    string Id,
    string Name,
    string ArtistName,
    string? ImgUrl,
    string? ExternalUrl);
