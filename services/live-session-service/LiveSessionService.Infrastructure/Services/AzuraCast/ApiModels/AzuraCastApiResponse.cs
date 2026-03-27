using System.Text.Json.Serialization;

namespace LiveSessionService.Infrastructure.Services.AzuraCast.ApiModels;

// Matches: GET /api/nowplaying/{station_id}
internal sealed class AzuraCastApiResponse
{
    [JsonPropertyName("station")]
    public AzuraCastApiStation? Station { get; set; }

    [JsonPropertyName("listeners")]
    public AzuraCastApiListeners? Listeners { get; set; }

    [JsonPropertyName("live")]
    public AzuraCastApiLive? Live { get; set; }

    [JsonPropertyName("now_playing")]
    public AzuraCastApiNowPlaying? NowPlaying { get; set; }

    [JsonPropertyName("playing_next")]
    public AzuraCastApiNowPlaying? PlayingNext { get; set; }

    [JsonPropertyName("song_history")]
    public List<AzuraCastApiSongHistory>? SongHistory { get; set; }

    [JsonPropertyName("is_online")]
    public bool IsOnline { get; set; }
}

internal sealed class AzuraCastApiStation
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("shortcode")]
    public string? ShortCode { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("listen_url")]
    public string? ListenUrl { get; set; }

    [JsonPropertyName("public_player_url")]
    public string? PublicPlayerUrl { get; set; }

    [JsonPropertyName("is_public")]
    public bool IsPublic { get; set; }

    [JsonPropertyName("requests_enabled")]
    public bool RequestsEnabled { get; set; }

    [JsonPropertyName("hls_enabled")]
    public bool HlsEnabled { get; set; }

    [JsonPropertyName("hls_url")]
    public string? HlsUrl { get; set; }

    [JsonPropertyName("mounts")]
    public List<AzuraCastApiMount>? Mounts { get; set; }
}

internal sealed class AzuraCastApiLive
{
    [JsonPropertyName("is_live")]
    public bool IsLive { get; set; }

    [JsonPropertyName("streamer_name")]
    public string? StreamerName { get; set; }
}

internal sealed class AzuraCastApiListeners
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("unique")]
    public int Unique { get; set; }

    [JsonPropertyName("current")]
    public int Current { get; set; }
}

internal sealed class AzuraCastApiNowPlaying
{
    [JsonPropertyName("sh_id")]
    public long ShId { get; set; }

    [JsonPropertyName("played_at")]
    public double PlayedAt { get; set; }

    [JsonPropertyName("duration")]
    public double Duration { get; set; }

    [JsonPropertyName("elapsed")]
    public double Elapsed { get; set; }

    [JsonPropertyName("remaining")]
    public double Remaining { get; set; }

    [JsonPropertyName("is_request")]
    public bool IsRequest { get; set; }

    [JsonPropertyName("song")]
    public AzuraCastApiSong? Song { get; set; }
}

internal sealed class AzuraCastApiSong
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("artist")]
    public string? Artist { get; set; }

    [JsonPropertyName("album")]
    public string? Album { get; set; }

    [JsonPropertyName("genre")]
    public string? Genre { get; set; }

    [JsonPropertyName("art")]
    public string? Art { get; set; }

    [JsonPropertyName("lyrics")]
    public string? Lyrics { get; set; }
}

internal sealed class AzuraCastApiSongHistory
{
    [JsonPropertyName("sh_id")]
    public long ShId { get; set; }

    [JsonPropertyName("played_at")]
    public long PlayedAt { get; set; }

    [JsonPropertyName("duration")]
    public long? Duration { get; set; }

    [JsonPropertyName("is_request")]
    public bool IsRequest { get; set; }

    [JsonPropertyName("song")]
    public AzuraCastApiSong? Song { get; set; }
}

internal sealed class AzuraCastApiMount
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("bitrate")]
    public int? Bitrate { get; set; }

    [JsonPropertyName("format")]
    public string? Format { get; set; }

    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("is_default")]
    public bool IsDefault { get; set; }

    [JsonPropertyName("listeners")]
    public AzuraCastApiListeners? Listeners { get; set; }
}

// Matches: GET /api/stations (station list endpoint)
internal sealed class AzuraCastApiStationListResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("shortcode")]
    public string? ShortCode { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("listen_url")]
    public string? ListenUrl { get; set; }

    [JsonPropertyName("public_player_url")]
    public string? PublicPlayerUrl { get; set; }

    [JsonPropertyName("is_public")]
    public bool IsPublic { get; set; }

    [JsonPropertyName("hls_enabled")]
    public bool HlsEnabled { get; set; }

    [JsonPropertyName("hls_url")]
    public string? HlsUrl { get; set; }

    [JsonPropertyName("mounts")]
    public List<AzuraCastApiMount>? Mounts { get; set; }
}

// Matches: POST /api/station/{id}/playlists response
internal sealed class AzuraCastApiPlaylistResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    
    [JsonPropertyName("source")]
    public string? Source { get; set; }
    
    [JsonPropertyName("order")]
    public string? Order { get; set; }  // Changed from int to string (shuffle, sequential, random)
    
    [JsonPropertyName("is_enabled")]
    public bool IsEnabled { get; set; }
    
    [JsonPropertyName("include_in_requests")]
    public bool IncludeInRequests { get; set; }
    
    [JsonPropertyName("include_in_on_demand")]
    public bool IncludeInOnDemand { get; set; }
    
    [JsonPropertyName("weight")]
    public int Weight { get; set; }
}

// Matches: POST /api/station/{id}/files response
internal sealed class AzuraCastApiFileResponse
{
    [JsonPropertyName("unique_id")]
    public string? UniqueId { get; set; }

    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("artist")]
    public string? Artist { get; set; }

    [JsonPropertyName("album")]
    public string? Album { get; set; }

    [JsonPropertyName("length")]
    public double Length { get; set; }
}

// Matches: GET /api/station/{id}/files response
internal sealed class AzuraCastApiStationFileResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("unique_id")]
    public string? UniqueId { get; set; }

    [JsonPropertyName("song_id")]
    public string? SongId { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("artist")]
    public string? Artist { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("album")]
    public string? Album { get; set; }

    [JsonPropertyName("genre")]
    public string? Genre { get; set; }

    [JsonPropertyName("isrc")]
    public string? Isrc { get; set; }

    [JsonPropertyName("lyrics")]
    public string? Lyrics { get; set; }

    [JsonPropertyName("art")]
    public string? Art { get; set; }

    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("mtime")]
    public long Mtime { get; set; }

    [JsonPropertyName("uploaded_at")]
    public long UploadedAt { get; set; }

    [JsonPropertyName("art_updated_at")]
    public long ArtUpdatedAt { get; set; }

    [JsonPropertyName("length")]
    public double Length { get; set; }

    [JsonPropertyName("length_text")]
    public string? LengthText { get; set; }

    [JsonPropertyName("playlists")]
    public List<AzuraCastApiFilePlaylistInfo>? Playlists { get; set; }
}

internal sealed class AzuraCastApiFilePlaylistInfo
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("short_name")]
    public string? ShortName { get; set; }

    [JsonPropertyName("folder")]
    public string? Folder { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }
}


