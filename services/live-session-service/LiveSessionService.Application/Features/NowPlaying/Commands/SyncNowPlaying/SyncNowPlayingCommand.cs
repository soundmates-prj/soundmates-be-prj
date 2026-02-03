using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.NowPlaying;

namespace LiveSessionService.Application.Features.NowPlaying.Commands.SyncNowPlaying;

/// <summary>
/// Command to sync now playing data from AzuraCast
/// Can be triggered manually or by background service
/// </summary>
public sealed record SyncNowPlayingCommand(int SessionId) : ICommand<NowPlayingResult>;
