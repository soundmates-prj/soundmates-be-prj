using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.Music;

namespace LiveSessionService.Application.Features.Music.Commands.SyncMediaFiles;

public sealed record SyncMediaFilesCommand(Guid StationId) : ICommand<SyncMediaFilesResult>;
