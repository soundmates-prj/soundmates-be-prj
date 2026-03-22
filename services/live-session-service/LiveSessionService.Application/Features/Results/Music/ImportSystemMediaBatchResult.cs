namespace LiveSessionService.Application.Features.Results.Music;

public sealed class ImportSystemMediaBatchResult
{
    public Guid StationId { get; init; }
    public string StationName { get; init; } = null!;
    public int RequestedCount { get; init; }
    public int ImportedCount { get; init; }
    public int SkippedCount { get; init; }
    public int FailedCount { get; init; }
    public List<ImportedSystemMediaItemResult> ImportedItems { get; init; } = new();
    public List<string> Errors { get; init; } = new();
}

public sealed class ImportedSystemMediaItemResult
{
    public Guid MediaFileId { get; init; }
    public string Title { get; init; } = null!;
    public string StationMediaUniqueId { get; init; } = null!;
}
