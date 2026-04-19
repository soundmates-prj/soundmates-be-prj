using LiveSessionService.Application.Abstractions.Messaging;

namespace LiveSessionService.Application.Features.LiveSessions.Commands.ReloadBroadcast;

/// <summary>
/// Command to reload the AzuraCast station broadcast for a live session
/// </summary>
public sealed record ReloadSessionBroadcastCommand(Guid SessionId) : ICommand<bool>;
