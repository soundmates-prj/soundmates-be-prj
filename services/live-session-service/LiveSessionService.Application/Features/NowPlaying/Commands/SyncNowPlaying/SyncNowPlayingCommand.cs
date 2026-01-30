using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results;

namespace LiveSessionService.Application.Features.NowPlaying.Commands.SyncNowPlaying;

/// <summary>
/// Command to sync now playing data from AzuraCast
/// Can be triggered manually or by background service
/// </summary>
public sealed record SyncNowPlayingCommand(Guid SessionId) : ICommand<NowPlayingResult>;
