using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.SongRequests;

namespace LiveSessionService.Application.Features.SongRequests.Queries.GetAllSongRequests;

public sealed record GetAllSongRequestsQuery(
    string? Status,
    int Page,
    int PageSize) : IQuery<PagedResult<SongRequestResult>>;
