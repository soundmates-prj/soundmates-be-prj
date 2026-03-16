using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.Playlists;

namespace LiveSessionService.Application.Features.Playlists.Queries.GetPlaylistTracks;

public sealed record GetPlaylistTracksQuery(Guid PlaylistId) : IQuery<List<PlaylistMediaResult>>;
