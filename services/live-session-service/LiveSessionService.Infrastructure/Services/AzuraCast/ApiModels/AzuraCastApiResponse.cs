using System.Text.Json.Serialization;

namespace LiveSessionService.Infrastructure.Services.AzuraCast.ApiModels;

/// <summary>
/// Internal API response model - matches AzuraCast JSON structure
/// Không expose ra ngoài Infrastructure
/// </summary>
internal sealed class AzuraCastApiResponse
{
    [JsonPropertyName("station")]
    public AzuraCastApiStation? Station { get; set; }
    
    [JsonPropertyName("now_playing")]
    public AzuraCastApiNowPlaying? NowPlaying { get; set; }
    
    [JsonPropertyName("song_history")]
    public List<AzuraCastApiSongHistory>? SongHistory { get; set; }
}

internal sealed class AzuraCastApiStation
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("shortcode")]
    public string? ShortCode { get; set; }
    
    [JsonPropertyName("listen_url")]
    public string? ListenUrl { get; set; }
    
    [JsonPropertyName("listeners")]
    public AzuraCastApiListeners? Listeners { get; set; }
}

internal sealed class AzuraCastApiNowPlaying
{
    [JsonPropertyName("sh_id")]
    public long? ShId { get; set; }
    
    [JsonPropertyName("song")]
    public AzuraCastApiSong? Song { get; set; }
    
    [JsonPropertyName("played_at")]
    public long? PlayedAt { get; set; }
    
    [JsonPropertyName("duration")]
    public long? Duration { get; set; }
    
    [JsonPropertyName("listeners")]
    public int? Listeners { get; set; }
    
    [JsonPropertyName("is_request")]
    public bool IsRequest { get; set; }
}

internal sealed class AzuraCastApiSong
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    
    [JsonPropertyName("text")]
    public string? Text { get; set; }
    
    [JsonPropertyName("artist")]
    public string? Artist { get; set; }
    
    [JsonPropertyName("title")]
    public string? Title { get; set; }
    
    [JsonPropertyName("album")]
    public string? Album { get; set; }
    
    [JsonPropertyName("art")]
    public string? Art { get; set; }
}

internal sealed class AzuraCastApiSongHistory
{
    [JsonPropertyName("sh_id")]
    public long? ShId { get; set; }
    
    [JsonPropertyName("played_at")]
    public long? PlayedAt { get; set; }
    
    [JsonPropertyName("song")]
    public AzuraCastApiSong? Song { get; set; }
}

internal sealed class AzuraCastApiListeners
{
    [JsonPropertyName("current")]
    public int Current { get; set; }
    
    [JsonPropertyName("unique")]
    public int Unique { get; set; }
}

/// <summary>
/// Station list response from /api/stations endpoint
/// </summary>
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

    [JsonPropertyName("is_enabled")]
    public bool IsEnabled { get; set; }

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

    [JsonPropertyName("listeners")]
    public AzuraCastApiListeners? Listeners { get; set; }

    [JsonPropertyName("mounts")]
    public List<AzuraCastApiMount>? Mounts { get; set; }
}

/// <summary>
/// Mount point information
/// </summary>
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

