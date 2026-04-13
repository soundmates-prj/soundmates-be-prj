namespace LiveSessionService.Application.Features.Common.AzuraCast.Models;

public sealed class AzuraCastCurrentSongData
{
    public long ShId { get; init; }
    public AzuraCastSongData? Song { get; init; }
    public double PlayedAt { get; init; }
    public double Duration { get; init; }
    public double Elapsed { get; init; }
    public double Remaining { get; init; }
    public bool IsRequest { get; init; }
}
