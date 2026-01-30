using System.Net.Http.Json;
using LiveSessionService.Application.Features.Common;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Infrastructure.Services.AzuraCast;

/// <summary>
/// Implementation of IAzuraCastService
/// HTTP client for AzuraCast API
/// </summary>
public sealed class AzuraCastService : IAzuraCastService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AzuraCastService> _logger;

    public AzuraCastService(HttpClient httpClient, ILogger<AzuraCastService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<AzuraCastNowPlayingData?> GetNowPlayingAsync(
        string baseUrl,
        int stationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Build URL with integer station ID
            var url = $"{baseUrl.TrimEnd('/')}/api/nowplaying/{stationId}";
            _logger.LogInformation("Fetching now playing from AzuraCast: {Url} (Station ID: {StationId})", url, stationId);

            var response = await _httpClient.GetFromJsonAsync<AzuraCastApiResponse>(
                url,
                cancellationToken);

            if (response == null)
            {
                _logger.LogWarning("Received null response from AzuraCast API");
                return null;
            }

            // Map API response to our data model
            var result = new AzuraCastNowPlayingData
            {
                Station = response.Station != null ? new AzuraCastStationData
                {
                    Id = response.Station.Id,
                    Name = response.Station.Name ?? "Unknown",
                    ShortCode = response.Station.ShortCode,
                    Listeners = response.Station.Listeners != null ? new AzuraCastListenersData
                    {
                        Current = response.Station.Listeners.Current,
                        Unique = response.Station.Listeners.Unique
                    } : null
                } : null,
                
                NowPlaying = response.NowPlaying != null ? new AzuraCastCurrentSongData
                {
                    ShId = response.NowPlaying.ShId,
                    PlayedAt = response.NowPlaying.PlayedAt,
                    Duration = response.NowPlaying.Duration,
                    Listeners = response.NowPlaying.Listeners,
                    Song = response.NowPlaying.Song != null ? new AzuraCastSongData
                    {
                        Id = response.NowPlaying.Song.Id,
                        Text = response.NowPlaying.Song.Text,
                        Artist = response.NowPlaying.Song.Artist,
                        Title = response.NowPlaying.Song.Title,
                        Album = response.NowPlaying.Song.Album,
                        Art = response.NowPlaying.Song.Art
                    } : null
                } : null,
                
                SongHistory = response.SongHistory?.Select(h => new AzuraCastSongHistoryData
                {
                    ShId = h.ShId,
                    PlayedAt = h.PlayedAt,
                    Song = h.Song != null ? new AzuraCastSongData
                    {
                        Id = h.Song.Id,
                        Text = h.Song.Text,
                        Artist = h.Song.Artist,
                        Title = h.Song.Title,
                        Album = h.Song.Album,
                        Art = h.Song.Art
                    } : null
                }).ToList()
            };

            _logger.LogInformation(
                "Successfully fetched now playing from AzuraCast for station {StationId}",
                stationId);

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching now playing from AzuraCast: {Message}", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching now playing from AzuraCast");
            throw;
        }
    }
}

/// <summary>
/// Internal API response models (matches actual AzuraCast JSON structure)
/// These are snake_case in the API but we map them to PascalCase properties
/// </summary>
internal sealed class AzuraCastApiResponse
{
    public AzuraCastApiStation? Station { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("now_playing")]
    public AzuraCastApiNowPlaying? NowPlaying { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("song_history")]
    public List<AzuraCastApiSongHistory>? SongHistory { get; set; }
}

internal sealed class AzuraCastApiStation
{
    public int Id { get; set; }
    public string? Name { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("shortcode")]
    public string? ShortCode { get; set; }
    
    public AzuraCastApiListeners? Listeners { get; set; }
}

internal sealed class AzuraCastApiNowPlaying
{
    [System.Text.Json.Serialization.JsonPropertyName("sh_id")]
    public long? ShId { get; set; }
    
    public AzuraCastApiSong? Song { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("played_at")]
    public long? PlayedAt { get; set; }
    
    public long? Duration { get; set; }
    public int? Listeners { get; set; }
}

internal sealed class AzuraCastApiSong
{
    public string? Id { get; set; }
    public string? Text { get; set; }
    public string? Artist { get; set; }
    public string? Title { get; set; }
    public string? Album { get; set; }
    public string? Art { get; set; }
}

internal sealed class AzuraCastApiSongHistory
{
    [System.Text.Json.Serialization.JsonPropertyName("sh_id")]
    public long? ShId { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("played_at")]
    public long? PlayedAt { get; set; }
    
    public AzuraCastApiSong? Song { get; set; }
}

internal sealed class AzuraCastApiListeners
{
    public int Current { get; set; }
    public int Unique { get; set; }
}
