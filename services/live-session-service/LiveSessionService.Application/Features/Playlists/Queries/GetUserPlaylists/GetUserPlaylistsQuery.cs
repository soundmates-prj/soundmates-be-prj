using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.Playlists;

namespace LiveSessionService.Application.Features.Playlists.Queries.GetUserPlaylists;

public sealed record GetUserPlaylistsQuery(Guid UserId) : IQuery<List<UserPlaylistResult>>;
