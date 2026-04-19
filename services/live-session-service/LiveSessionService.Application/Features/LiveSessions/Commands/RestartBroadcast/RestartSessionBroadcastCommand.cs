using LiveSessionService.Application.Abstractions.Messaging;

namespace LiveSessionService.Application.Features.LiveSessions.Commands.RestartBroadcast;

/// <summary>
/// Command to restart the AzuraCast station broadcast for a live session
/// </summary>
public sealed record RestartSessionBroadcastCommand(Guid SessionId) : ICommand<bool>;
