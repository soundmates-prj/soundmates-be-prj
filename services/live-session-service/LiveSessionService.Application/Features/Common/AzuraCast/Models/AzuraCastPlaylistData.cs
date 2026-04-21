namespace LiveSessionService.Application.Features.Common.AzuraCast.Models;

public sealed class AzuraCastPlaylistData
{
    public int Id { get; init; }
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public string? Type { get; init; }
    public string? Source { get; init; }
    public string? Order { get; init; }  // Changed from int to string (shuffle, sequential, random)
    public bool IsEnabled { get; init; }
    public bool IncludeInRequests { get; init; }
    public bool IncludeInOnDemand { get; init; }
    public int Weight { get; init; }
}
