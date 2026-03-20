using System.Text.Json.Serialization;

namespace AuthQueryService.Application.DTOs.Spotify
{
    // ─────────────────────────────────────────────────────────────
    //  Spotify Web API DTOs — Application layer (no infrastructure)
    //  Kept in Application so Query handlers can reference them.
    // ─────────────────────────────────────────────────────────────

    public sealed class SpotifySearchResult
    {
        [JsonPropertyName("tracks")]
        public SpotifyPaginatedResult<SpotifyTrackDto>? Tracks { get; set; }

        [JsonPropertyName("artists")]
        public SpotifyPaginatedResult<SpotifyArtistDto>? Artists { get; set; }

        [JsonPropertyName("albums")]
        public SpotifyPaginatedResult<SpotifyAlbumDto>? Albums { get; set; }
    }

    public sealed class SpotifyPaginatedResult<T>
    {
        [JsonPropertyName("items")]
        public List<T> Items { get; set; } = [];

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; }

        [JsonPropertyName("offset")]
        public int Offset { get; set; }

        [JsonPropertyName("next")]
        public string? Next { get; set; }

        [JsonPropertyName("previous")]
        public string? Previous { get; set; }
    }

    // ── Track ──────────────────────────────────────────────
    public sealed class SpotifyTrackDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;

        [JsonPropertyName("artists")]
        public List<SpotifySimpleArtistDto> Artists { get; set; } = [];

        [JsonPropertyName("album")]
        public SpotifySimpleAlbumDto? Album { get; set; }

        [JsonPropertyName("duration_ms")]
        public int DurationMs { get; set; }

        [JsonPropertyName("preview_url")]
        public string? PreviewUrl { get; set; }

        [JsonPropertyName("explicit")]
        public bool Explicit { get; set; }

        [JsonPropertyName("popularity")]
        public int Popularity { get; set; }

        [JsonPropertyName("external_urls")]
        public SpotifyExternalUrls? ExternalUrls { get; set; }

        [JsonPropertyName("uri")]
        public string? Uri { get; set; }

        [JsonIgnore] public string ArtistName => string.Join(", ", Artists.Select(a => a.Name));
        [JsonIgnore] public string? AlbumName => Album?.Name;
        [JsonIgnore] public string? ImageUrl => Album?.Images?.FirstOrDefault()?.Url;
    }

    // ── Artist ─────────────────────────────────────────────
    public sealed class SpotifyArtistDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;

        [JsonPropertyName("genres")]
        public List<string> Genres { get; set; } = [];

        [JsonPropertyName("popularity")]
        public int Popularity { get; set; }

        [JsonPropertyName("images")]
        public List<SpotifyImageDto> Images { get; set; } = [];

        [JsonPropertyName("external_urls")]
        public SpotifyExternalUrls? ExternalUrls { get; set; }

        [JsonPropertyName("followers")]
        public SpotifyFollowers? Followers { get; set; }

        [JsonPropertyName("uri")]
        public string? Uri { get; set; }

        [JsonIgnore] public string? ImageUrl => Images?.FirstOrDefault()?.Url;
    }

    // ── Album ──────────────────────────────────────────────
    public sealed class SpotifyAlbumDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;

        [JsonPropertyName("album_type")]
        public string? AlbumType { get; set; }

        [JsonPropertyName("total_tracks")]
        public int TotalTracks { get; set; }

        [JsonPropertyName("release_date")]
        public string? ReleaseDate { get; set; }

        [JsonPropertyName("images")]
        public List<SpotifyImageDto> Images { get; set; } = [];

        [JsonPropertyName("artists")]
        public List<SpotifySimpleArtistDto> Artists { get; set; } = [];

        [JsonPropertyName("external_urls")]
        public SpotifyExternalUrls? ExternalUrls { get; set; }

        [JsonPropertyName("uri")]
        public string? Uri { get; set; }

        [JsonIgnore] public string ArtistName => string.Join(", ", Artists.Select(a => a.Name));
        [JsonIgnore] public string? ImageUrl => Images?.FirstOrDefault()?.Url;
    }

    // ── Shared ─────────────────────────────────────────────
    public sealed class SpotifySimpleArtistDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;

        [JsonPropertyName("external_urls")]
        public SpotifyExternalUrls? ExternalUrls { get; set; }
    }

    public sealed class SpotifySimpleAlbumDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;

        [JsonPropertyName("images")]
        public List<SpotifyImageDto> Images { get; set; } = [];

        [JsonPropertyName("release_date")]
        public string? ReleaseDate { get; set; }

        [JsonPropertyName("external_urls")]
        public SpotifyExternalUrls? ExternalUrls { get; set; }
    }

    public sealed class SpotifyImageDto
    {
        [JsonPropertyName("url")]
        public string Url { get; set; } = null!;

        [JsonPropertyName("height")]
        public int? Height { get; set; }

        [JsonPropertyName("width")]
        public int? Width { get; set; }
    }

    public sealed class SpotifyExternalUrls
    {
        [JsonPropertyName("spotify")]
        public string? Spotify { get; set; }
    }

    public sealed class SpotifyFollowers
    {
        [JsonPropertyName("total")]
        public int Total { get; set; }
    }

    public sealed class SpotifySeveralTracksResult
    {
        [JsonPropertyName("tracks")]
        public List<SpotifyTrackDto> Tracks { get; set; } = [];
    }
}
