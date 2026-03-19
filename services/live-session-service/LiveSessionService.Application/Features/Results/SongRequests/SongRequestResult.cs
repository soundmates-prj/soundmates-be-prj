namespace LiveSessionService.Application.Features.Results.SongRequests;

public sealed class SongRequestResult
{
    public Guid Id { get; init; }
    public Guid LiveSessionId { get; init; }
    public Guid MediaFileId { get; init; }
    public Guid RequestedByUserId { get; init; }
    public string Status { get; init; } = null!;
    public Guid? ReviewedByUserId { get; init; }
    public DateTime RequestedAt { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public string? Message { get; init; }
    public string? RejectReason { get; init; }
    public string SongTitle { get; init; } = null!;
    public string? SongArtist { get; init; }
    public string? SongAlbum { get; init; }
}
