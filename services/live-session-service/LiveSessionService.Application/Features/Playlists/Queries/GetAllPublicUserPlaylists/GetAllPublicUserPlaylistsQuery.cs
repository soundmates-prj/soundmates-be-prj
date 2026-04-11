using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.Playlists;

namespace LiveSessionService.Application.Features.Playlists.Queries.GetAllPublicUserPlaylists;

public sealed record GetAllPublicUserPlaylistsQuery() : IQuery<List<UserPlaylistResult>>;
