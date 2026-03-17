using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.Music;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Application.Features.Music.Queries.GetAllMediaFiles;

public sealed class GetAllMediaFilesHandler
    : IQueryHandler<GetAllMediaFilesQuery, List<MusicResult>>
{
    private readonly IMediaFileRepository _mediaFiles;
    private readonly ILogger<GetAllMediaFilesHandler> _logger;

    public GetAllMediaFilesHandler(
        IMediaFileRepository mediaFiles,
        ILogger<GetAllMediaFilesHandler> logger)
    {
        _mediaFiles = mediaFiles;
        _logger = logger;
    }

    public async Task<Result<List<MusicResult>>> Handle(
        GetAllMediaFilesQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Fetching all media files from catalog");

            var entities = await _mediaFiles.GetAllAsync(cancellationToken);

            var results = entities
                .Select(m => new MusicResult
                {
                    Id         = m.Id,
                    //StationId  = Guid.Empty, // not station-specific in this listing
                    Title      = m.Title,
                    Artist     = m.Artist ?? string.Empty,
                    Album      = m.Album,
                    ArtworkUrl = m.ArtUrl,
                    Duration   = m.DurationSeconds,
                    FileUrl    = m.FilePath,
                    FileType   = m.FileType,
                    FileSize   = m.FileSizeBytes,
                    UploadedAt = m.UploadedAt
                })
                .ToList();

            _logger.LogInformation("Found {Count} media files in catalog", results.Count);

            return Result<List<MusicResult>>.Success(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get media files from catalog");
            return Result<List<MusicResult>>.Failure(
                "Failed to retrieve media files from catalog",
                ErrorCode.InternalServerError);
        }
    }
}

