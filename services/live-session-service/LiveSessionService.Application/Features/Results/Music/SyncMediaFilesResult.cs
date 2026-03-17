namespace LiveSessionService.Application.Features.Results.Music;

public sealed class SyncMediaFilesResult
{
    public Guid StationId { get; init; }
    public string StationName { get; init; } = null!;
    public int TotalFilesInAzuraCast { get; init; }
    public int NewFilesSynced { get; init; }
    public int UpdatedFiles { get; init; }
    public int UnchangedFiles { get; init; }
}
