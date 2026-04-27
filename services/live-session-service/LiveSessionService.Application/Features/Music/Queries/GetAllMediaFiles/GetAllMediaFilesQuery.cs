using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.Music;

namespace LiveSessionService.Application.Features.Music.Queries.GetAllMediaFiles;

/// <summary>
/// Query to get all media files from local catalog.
/// </summary>
public sealed record GetAllMediaFilesQuery : IQuery<List<MusicResult>>;

