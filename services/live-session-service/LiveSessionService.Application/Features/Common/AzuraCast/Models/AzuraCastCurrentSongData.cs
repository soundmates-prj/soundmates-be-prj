namespace LiveSessionService.Application.Features.Common.AzuraCast.Models;

public sealed class AzuraCastCurrentSongData
{
    public long? ShId { get; init; } // Song history ID
    public AzuraCastSongData? Song { get; init; }
    public long? PlayedAt { get; init; } // Unix timestamp
    public long? Duration { get; init; }
    public int? Listeners { get; init; }
}
