namespace LiveSessionService.Application.Features.Common.AzuraCast.Models;

public sealed class AzuraCastMediaData
{
    /// <summary>AzuraCast unique_id — used to reference the file in subsequent API calls</summary>
    public string UniqueId { get; init; } = null!;
    public string Path { get; init; } = null!;
    public string Title { get; init; } = null!;
    public string? Artist { get; init; }
    public string? Album { get; init; }
    public int DurationSeconds { get; init; }
}
