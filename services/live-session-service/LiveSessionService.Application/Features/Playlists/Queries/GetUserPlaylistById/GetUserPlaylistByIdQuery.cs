using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.Playlists;

namespace LiveSessionService.Application.Features.Playlists.Queries.GetUserPlaylistById;

public sealed record GetUserPlaylistByIdQuery(Guid PlaylistId, Guid UserId) : IQuery<UserPlaylistResult>;
