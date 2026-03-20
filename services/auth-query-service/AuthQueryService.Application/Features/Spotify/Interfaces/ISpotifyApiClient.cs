using AuthQueryService.Application.DTOs.Spotify;

namespace AuthQueryService.Application.Abstractions
{
    /// <summary>
    /// Abstraction for Spotify Web API calls.
    /// Defined in Application → implemented in Infrastructure.
    /// Uses Client Credentials Flow (server-to-server, no user login required).
    /// </summary>
    public interface ISpotifyApiClient
    {
        Task<SpotifySearchResult> SearchAsync(
            string query,
            string type = "track",
            int limit = 10,
            int offset = 0,
            CancellationToken ct = default,
            string? userAccessToken = null);

        Task<SpotifyTrackDto?> GetTrackAsync(string trackId, CancellationToken ct = default);
        Task<SpotifyArtistDto?> GetArtistAsync(string artistId, CancellationToken ct = default);
        Task<SpotifyAlbumDto?> GetAlbumAsync(string albumId, CancellationToken ct = default);
        Task<List<SpotifyTrackDto>> GetSeveralTracksAsync(IEnumerable<string> trackIds, CancellationToken ct = default);
    }
}
