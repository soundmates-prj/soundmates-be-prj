using System.Net.Http.Json;
using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Features.Common.AzuraCast.Models;
using LiveSessionService.Infrastructure.ExternalServices.AzuraCast.ApiModels;
using LiveSessionService.Infrastructure.ExternalServices.AzuraCast.Mappers;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Infrastructure.ExternalServices.AzuraCast;

/// <summary>
/// HTTP client for AzuraCast API
/// BaseUrl ???c config qua HttpClient DI
/// Ch? lo vi?c call API và map response
/// </summary>
public sealed class AzuraCastClient : IAzuraCastClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AzuraCastClient> _logger;

    public AzuraCastClient(HttpClient httpClient, ILogger<AzuraCastClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<AzuraCastStationListData>> GetStationsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            const string endpoint = "api/stations";
            _logger.LogInformation("Fetching stations list from AzuraCast");

            // Call API - deserialize to internal API model
            var apiResponse = await _httpClient.GetFromJsonAsync<List<AzuraCastApiStationListResponse>>(
                endpoint,
                cancellationToken);

            if (apiResponse == null || apiResponse.Count == 0)
            {
                _logger.LogWarning("Received empty or null response from AzuraCast stations API");
                return new List<AzuraCastStationListData>();
            }

            // Map internal API model to Application model
            var applicationModels = apiResponse
                .Select(station => station.ToApplicationModel())
                .ToList();

            _logger.LogInformation(
                "Successfully fetched {Count} stations from AzuraCast",
                applicationModels.Count);

            return applicationModels;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, 
                "HTTP error fetching stations from AzuraCast: {Message}", 
                ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error fetching stations from AzuraCast");
            throw;
        }
    }

    public async Task<AzuraCastNowPlayingData?> GetNowPlayingAsync(
        int stationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoint = $"api/nowplaying/{stationId}";
            _logger.LogInformation(
                "Fetching now playing from AzuraCast for station {StationId}", 
                stationId);

            // Call API - deserialize to internal API model
            var apiResponse = await _httpClient.GetFromJsonAsync<AzuraCastApiResponse>(
                endpoint,
                cancellationToken);

            if (apiResponse == null)
            {
                _logger.LogWarning(
                    "Received null response from AzuraCast API for station {StationId}", 
                    stationId);
                return null;
            }

            // Map internal API model ? Application model
            var applicationModel = apiResponse.ToApplicationModel();

            _logger.LogInformation(
                "Successfully fetched now playing from AzuraCast for station {StationId}",
                stationId);

            return applicationModel;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, 
                "HTTP error fetching now playing from AzuraCast for station {StationId}: {Message}", 
                stationId, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error fetching now playing from AzuraCast for station {StationId}", 
                stationId);
            throw;
        }
    }
}
