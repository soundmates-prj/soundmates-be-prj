using System.Text.Json.Serialization;

namespace LiveSessionService.Infrastructure.ExternalServices.AzuraCast.ApiModels;

/// <summary>
/// Response model for AzuraCast /api/stations endpoint
/// Returns array of stations
/// </summary>
internal sealed class AzuraCastApiStationListResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("shortcode")]
    public string? Shortcode { get; set; }
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("frontend")]
    public string? Frontend { get; set; }
    
    [JsonPropertyName("backend")]
    public string? Backend { get; set; }
    
    [JsonPropertyName("listen_url")]
    public string? ListenUrl { get; set; }
    
    [JsonPropertyName("url")]
    public string? Url { get; set; }
    
    [JsonPropertyName("public_player_url")]
    public string? PublicPlayerUrl { get; set; }
    
    [JsonPropertyName("playlist_pls_url")]
    public string? PlaylistPlsUrl { get; set; }
    
    [JsonPropertyName("playlist_m3u_url")]
    public string? PlaylistM3uUrl { get; set; }
    
    [JsonPropertyName("is_public")]
    public bool IsPublic { get; set; }
    
    [JsonPropertyName("mounts")]
    public List<AzuraCastApiMount>? Mounts { get; set; }
    
    [JsonPropertyName("hls_enabled")]
    public bool HlsEnabled { get; set; }
    
    [JsonPropertyName("hls_url")]
    public string? HlsUrl { get; set; }
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
    
    [JsonPropertyName("listeners")]
    public AzuraCastApiListeners? Listeners { get; set; }
    
    [JsonPropertyName("path")]
    public string? Path { get; set; }
    
    [JsonPropertyName("is_default")]
    public bool IsDefault { get; set; }
}
