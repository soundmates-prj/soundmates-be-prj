using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.SongRequests;

namespace LiveSessionService.Application.Features.SongRequests.Queries.GetSongRequestsBySession;

public sealed record GetSongRequestsBySessionQuery(Guid LiveSessionId, string? Status)
    : IQuery<List<SongRequestResult>>;
