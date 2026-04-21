using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.Playlists;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Application.Features.Playlists.Queries.GetPlaylistsByStation;

/// <summary>
/// Handler for getting all playlists for a specific station
/// </summary>
public sealed class GetPlaylistsByStationHandler : IQueryHandler<GetPlaylistsByStationQuery, List<PlaylistResult>>
{
    private readonly IStationPlaylistRepository _playlistRepository;
    private readonly IAzuraCastStationRepository _stationRepository;
    private readonly ILogger<GetPlaylistsByStationHandler> _logger;

    public GetPlaylistsByStationHandler(
        IStationPlaylistRepository playlistRepository,
        IAzuraCastStationRepository stationRepository,
        ILogger<GetPlaylistsByStationHandler> logger)
    {
        _playlistRepository = playlistRepository;
        _stationRepository = stationRepository;
        _logger = logger;
    }

    public async Task<Result<List<PlaylistResult>>> Handle(
        GetPlaylistsByStationQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting playlists for station {StationId}", query.StationId);

            // Verify station exists
            var station = await _stationRepository.GetByIdAsync(query.StationId, cancellationToken);
            if (station == null)
            {
                _logger.LogWarning("Station {StationId} not found", query.StationId);
                return Result<List<PlaylistResult>>.Failure(
                    "Station not found",
                    ErrorCode.NotFound);
            }

            // Get playlists from database
            var playlists = await _playlistRepository.GetByStationIdAsync(query.StationId, cancellationToken);

            var results = playlists.Select(p => new PlaylistResult
            {
                Id = p.Id,
                StationId = p.AzuraCastStationId,
                PlaylistName = p.PlaylistName,
                Description = p.Description,
                IsAutoPlay = p.Type == Domain.Enums.PlaylistType.Default,
                IncludeInRequests = p.IncludeInRequests,
                SongPlaybackOrder = p.SongPlaybackOrder.ToString(),
                TotalTracks = p.Media?.Count ?? 0,
                TotalDuration = p.Media?.Sum(m => m.DurationSeconds) ?? 0,
                CreatedAt = p.CreatedAt
            }).ToList();

            _logger.LogInformation("Found {Count} playlists for station {StationId}", results.Count, query.StationId);

            return Result<List<PlaylistResult>>.Success(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get playlists for station {StationId}", query.StationId);
            return Result<List<PlaylistResult>>.Failure(
                "Failed to retrieve playlists",
                ErrorCode.InternalServerError);
        }
    }
}
