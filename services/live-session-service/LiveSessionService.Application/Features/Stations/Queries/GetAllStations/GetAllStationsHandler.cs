using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.Stations;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Application.Features.Stations.Queries.GetAllStations;

/// <summary>
/// Handler for getting all stations from local database
/// </summary>
public sealed class GetAllStationsHandler : IQueryHandler<GetAllStationsQuery, List<StationResult>>
{
    private readonly IAzuraCastStationRepository _stationRepository;
    private readonly ILogger<GetAllStationsHandler> _logger;

    public GetAllStationsHandler(
        IAzuraCastStationRepository stationRepository,
        ILogger<GetAllStationsHandler> logger)
    {
        _stationRepository = stationRepository;
        _logger = logger;
    }

    public async Task<Result<List<StationResult>>> Handle(GetAllStationsQuery query,CancellationToken cancellationToken)
    {
        try
        {

            // Query het tu database cua minh de lay tat ca cac station
            _logger.LogInformation("Getting all stations from database");

            var stations = await _stationRepository.GetAllEnabledAsync(cancellationToken);

            var results = stations.Select(s => new StationResult
            {
                Id = s.Id,
                ExternalStationId = s.ExternalStationId,
                StationName = s.StationName,
                StationShortcode = s.StationShortcode,
                Description = s.Description,
                StreamUrl = s.StreamUrl,
                PublicPlayerUrl = s.PublicPlayerUrl,
                IsEnabled = s.IsEnabled,
                LastSyncedAt = s.LastSyncedAt,
                SyncStatus = s.SyncStatus.ToString(),
                Mounts = s.Mounts.Select(m => new MountResult
                {
                    ExternalMountId = m.ExternalMountId,
                    MountName = m.MountName,
                    MountPath = m.MountPath,
                    MountUrl = m.MountUrl,
                    IsDefault = m.IsDefault,
                    Bitrate = m.Bitrate,
                    Format = m.Format,
                    CurrentListeners = m.CurrentListeners
                }).ToList()
            }).ToList();

            _logger.LogInformation("Found {Count} stations in database", results.Count);

            return Result<List<StationResult>>.Success(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get stations from database");
            return Result<List<StationResult>>.Failure(
                "Failed to retrieve stations from database",
                ErrorCode.InternalServerError);
        }
    }
}
