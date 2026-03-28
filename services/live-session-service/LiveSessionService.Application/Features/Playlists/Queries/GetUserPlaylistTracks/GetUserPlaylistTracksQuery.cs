using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.Playlists;

namespace LiveSessionService.Application.Features.Playlists.Queries.GetUserPlaylistTracks;

public sealed record GetUserPlaylistTracksQuery(Guid PlaylistId, Guid UserId) : IQuery<List<PlaylistMediaResult>>;
