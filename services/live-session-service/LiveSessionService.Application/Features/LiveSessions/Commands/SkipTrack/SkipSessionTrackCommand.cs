using LiveSessionService.Application.Abstractions.Messaging;

namespace LiveSessionService.Application.Features.LiveSessions.Commands.SkipTrack;

/// <summary>
/// Command to skip the currently playing track in a live session
/// </summary>
public sealed record SkipSessionTrackCommand(Guid SessionId) : ICommand<bool>;
