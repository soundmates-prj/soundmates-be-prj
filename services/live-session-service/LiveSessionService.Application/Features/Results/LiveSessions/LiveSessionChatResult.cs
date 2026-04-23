namespace LiveSessionService.Application.Features.Results.LiveSessions;

public sealed class LiveSessionChatResult
{
    public Guid Id { get; init; }
    public Guid LiveSessionId { get; init; }
    public Guid? UserId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? AvatarUrl { get; init; }
    public DateTime CreatedAt { get; init; }
}
