using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.Playlists;

namespace LiveSessionService.Application.Features.Playlists.Queries.GetPublicUserPlaylistsByUserId;

public sealed record GetPublicUserPlaylistsByUserIdQuery(Guid UserId) : IQuery<List<UserPlaylistResult>>;
