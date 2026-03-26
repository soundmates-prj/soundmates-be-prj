namespace LiveSessionService.Application.Features.Common.AzuraCast.Models;

public sealed class AzuraCastSongHistoryData
{
    public long ShId { get; init; }
    public long PlayedAt { get; init; }
    public long? Duration { get; init; }
    public bool IsRequest { get; init; }
    public AzuraCastSongData? Song { get; init; }
}


