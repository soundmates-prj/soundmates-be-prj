using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.SongRequests;

namespace LiveSessionService.Application.Features.SongRequests.Commands.ReviewSongRequest;

public sealed record ReviewSongRequestCommand(
    Guid SongRequestId,
    Guid ReviewedByUserId,
    bool IsApproved,
    string? RejectReason) : ICommand<SongRequestResult>;
