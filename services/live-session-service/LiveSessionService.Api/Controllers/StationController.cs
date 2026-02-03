using Microsoft.AspNetCore.Mvc;
using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Common.AzuraCast.Models;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.Stations;
using LiveSessionService.Application.Features.Stations.Commands.SyncStations;
using LiveSessionService.Application.Features.Stations.Queries.GetAllStations;
using LiveSessionService.Api.Models.Responses;

namespace LiveSessionService.Api.Controllers;

/// <summary>
/// API endpoints for AzuraCast station management
/// </summary>
[ApiController]
[Route("api/v1/stations")]
[Produces("application/json")]
public class StationController : ControllerBase
{
    private readonly IAzuraCastClient _azuraCastClient;
    private readonly ICommandHandler<SyncStationsCommand, SyncStationsResult> _syncStationsHandler;
    private readonly IQueryHandler<GetAllStationsQuery, List<StationResult>> _getAllStationsHandler;
    private readonly ILogger<StationController> _logger;

    public StationController(
        IAzuraCastClient azuraCastClient,
        ICommandHandler<SyncStationsCommand, SyncStationsResult> syncStationsHandler,
        IQueryHandler<GetAllStationsQuery, List<StationResult>> getAllStationsHandler,
        ILogger<StationController> logger)
    {
        _azuraCastClient = azuraCastClient;
        _syncStationsHandler = syncStationsHandler;
        _getAllStationsHandler = getAllStationsHandler;
        _logger = logger;
    }

    /// <summary>
    /// Get all stations from local database
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of stations in local database</returns>
    [HttpGet("local")]
    [ProducesResponseType(typeof(ApiResponse<List<StationResult>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<List<StationResult>>), 500)]
    public async Task<IActionResult> GetLocalStations(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Getting stations from local database");
            
            var query = new GetAllStationsQuery();
            var result = await _getAllStationsHandler.Handle(query, ct);

            if (!result.IsSuccess)
            {
                return StatusCode(500, ApiResponse<List<StationResult>>.FailureResponse(
                    result.ErrorMessage ?? "Failed to retrieve stations",
                    500));
            }
            
            return Ok(ApiResponse<List<StationResult>>.SuccessResponse(
                result.Data!, 
                "Stations retrieved successfully from local database"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stations from local database");
            
            return StatusCode(500, ApiResponse<List<StationResult>>.FailureResponse(
                "Failed to retrieve stations from local database",
                500));
        }
    }

    /// <summary>
    /// Get all stations from AzuraCast (real-time data)
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of available stations from AzuraCast</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<AzuraCastStationListData>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<List<AzuraCastStationListData>>), 500)]
    public async Task<IActionResult> GetStations(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Getting stations from AzuraCast");
            
            var stations = await _azuraCastClient.GetStationsAsync(ct);
            
            return Ok(ApiResponse<List<AzuraCastStationListData>>.SuccessResponse(
                stations, 
                "Stations retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stations from AzuraCast");
            
            return StatusCode(500, ApiResponse<List<AzuraCastStationListData>>.FailureResponse(
                "Failed to retrieve stations from AzuraCast",
                500));
        }
    }

    /// <summary>
    /// Sync all stations from AzuraCast to local database
    /// This will create new stations or update existing ones
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Sync statistics</returns>
    [HttpPost("sync")]
    [ProducesResponseType(typeof(ApiResponse<SyncStationsResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<SyncStationsResult>), 400)]
    [ProducesResponseType(typeof(ApiResponse<SyncStationsResult>), 500)]
    public async Task<IActionResult> SyncStations(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Starting station sync from AzuraCast");
            
            var command = new SyncStationsCommand();
            var result = await _syncStationsHandler.Handle(command, ct);

            if (!result.IsSuccess)
            {
                var statusCode = result.ErrorCode == Application.Enums.ErrorCode.BadRequest ? 400 : 500;
                return StatusCode(statusCode, ApiResponse<SyncStationsResult>.FailureResponse(
                    result.ErrorMessage ?? "Failed to sync stations",
                    statusCode));
            }
            
            var data = result.Data!;
            var message = $"Sync completed. Created: {data.CreatedStations}, Updated: {data.UpdatedStations}, Failed: {data.FailedStations}";
            
            return Ok(ApiResponse<SyncStationsResult>.SuccessResponse(
                data, 
                message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing stations from AzuraCast");
            
            return StatusCode(500, ApiResponse<SyncStationsResult>.FailureResponse(
                "Failed to sync stations from AzuraCast",
                500));
        }
    }

    /// <summary>
    /// Get a specific station by ID from AzuraCast
    /// </summary>
    /// <param name="stationId">The AzuraCast station ID (integer)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Station details with current now playing information</returns>
    [HttpGet("{stationId:int}")]
    [ProducesResponseType(typeof(ApiResponse<AzuraCastNowPlayingData>), 200)]
    [ProducesResponseType(typeof(ApiResponse<AzuraCastNowPlayingData>), 404)]
    [ProducesResponseType(typeof(ApiResponse<AzuraCastNowPlayingData>), 500)]
    public async Task<IActionResult> GetStation(int stationId, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Getting station {StationId} from AzuraCast", stationId);
            
            var nowPlaying = await _azuraCastClient.GetNowPlayingAsync(stationId, ct);
            
            if (nowPlaying == null)
            {
                return NotFound(ApiResponse<AzuraCastNowPlayingData>.FailureResponse(
                    $"Station with ID {stationId} not found",
                    404));
            }
            
            return Ok(ApiResponse<AzuraCastNowPlayingData>.SuccessResponse(
                nowPlaying, 
                "Station retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting station {StationId} from AzuraCast", stationId);
            
            return StatusCode(500, ApiResponse<AzuraCastNowPlayingData>.FailureResponse(
                "Failed to retrieve station from AzuraCast",
                500));
        }
    }
}
