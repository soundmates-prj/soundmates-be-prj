using Microsoft.AspNetCore.Mvc;
using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Features.Common.AzuraCast.Models;
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
    private readonly ILogger<StationController> _logger;

    public StationController(
        IAzuraCastClient azuraCastClient,
        ILogger<StationController> logger)
    {
        _azuraCastClient = azuraCastClient;
        _logger = logger;
    }

    /// <summary>
    /// Get all stations from AzuraCast
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of available stations</returns>
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
