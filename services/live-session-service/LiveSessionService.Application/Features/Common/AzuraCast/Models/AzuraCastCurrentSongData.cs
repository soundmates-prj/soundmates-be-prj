namespace LiveSessionService.Application.Features.Common.AzuraCast.Models;

public sealed class AzuraCastCurrentSongData
{
    public long ShId { get; init; }
    public AzuraCastSongData? Song { get; init; }
    public long PlayedAt { get; init; }
    public long Duration { get; init; }
    public long Elapsed { get; init; }
    public long Remaining { get; init; }
    public bool IsRequest { get; init; }
}
