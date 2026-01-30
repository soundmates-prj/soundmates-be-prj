namespace LiveSessionService.Application.Features.Common.AzuraCast.Models;

public sealed class AzuraCastSongData
{
    public string? Id { get; init; }
    public string? Text { get; init; } // Full text (Artist - Title)
    public string? Artist { get; init; }
    public string? Title { get; init; }
    public string? Album { get; init; }
    public string? Art { get; init; }
}
