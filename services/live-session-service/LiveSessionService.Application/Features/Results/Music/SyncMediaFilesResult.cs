namespace LiveSessionService.Application.Features.Results.Music;

public sealed class SyncMediaFilesResult
{
    public Guid StationId { get; init; }
    public string StationName { get; init; } = null!;
    public int TotalFilesInAzuraCast { get; init; }
    public int Created { get; init; }
    public int Updated { get; init; }
    public int Skipped { get; init; }
    public int Failed { get; init; }
    public List<string> Errors { get; init; } = [];
}
