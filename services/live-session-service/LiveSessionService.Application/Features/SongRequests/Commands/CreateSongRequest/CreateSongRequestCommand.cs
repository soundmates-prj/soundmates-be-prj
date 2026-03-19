using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.SongRequests;

namespace LiveSessionService.Application.Features.SongRequests.Commands.CreateSongRequest;

public sealed record CreateSongRequestCommand(
    Guid LiveSessionId,
    Guid MediaFileId,
    Guid RequestedByUserId,
    string? Message) : ICommand<SongRequestResult>;
