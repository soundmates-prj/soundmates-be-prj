namespace LiveSessionService.Application.Abstractions.ExternalServices;

/// <summary>
/// Interface for AzuraCast API client
/// Defined in Application layer, implemented in Infrastructure
/// </summary>
public interface IAzuraCastClient
{
    /// <summary>
    /// Gets now playing information for a station
    /// </summary>
    Task<AzuraCastNowPlayingDto?> GetNowPlayingAsync(
        string baseUrl, 
        int stationId, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// DTO from AzuraCast API response
/// </summary>
public class AzuraCastNowPlayingDto
{
    public AzuraCastStationDto? Station { get; set; }
    public AzuraCastCurrentSongDto? NowPlaying { get; set; }
    public List<AzuraCastSongHistoryDto>? SongHistory { get; set; }
}

public class AzuraCastStationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? ShortCode { get; set; }
    public AzuraCastListenersDto? Listeners { get; set; }
}

public class AzuraCastCurrentSongDto
{
    public long? ShId { get; set; } // Song history ID
    public AzuraCastSongDto? Song { get; set; }
    public long? PlayedAt { get; set; } // Unix timestamp
    public long? Duration { get; set; }
    public int? Listeners { get; set; }
}

public class AzuraCastSongDto
{
    public string? Id { get; set; }
    public string? Text { get; set; } // Full text (Artist - Title)
    public string? Artist { get; set; }
    public string? Title { get; set; }
    public string? Album { get; set; }
    public string? Art { get; set; } // Album art URL
}

public class AzuraCastSongHistoryDto
{
    public long ShId { get; set; }
    public long PlayedAt { get; set; }
    public int Duration { get; set; }
    public AzuraCastSongDto? Song { get; set; }
    public bool IsRequest { get; set; }
}

public class AzuraCastListenersDto
{
    public int Total { get; set; }
    public int Unique { get; set; }
    public int Current { get; set; }
}
