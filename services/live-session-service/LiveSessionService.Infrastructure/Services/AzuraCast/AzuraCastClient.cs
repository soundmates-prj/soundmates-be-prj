using System.Net;
using System.Net.Http.Json;
using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Exceptions;
using LiveSessionService.Application.Features.Common.AzuraCast.Models;
using LiveSessionService.Infrastructure.Services.AzuraCast.ApiModels;
using LiveSessionService.Infrastructure.Services.AzuraCast.Mappers;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Infrastructure.Services.AzuraCast;

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

    public async Task<AzuraCastPlaylistData?> CreatePlaylistAsync(
        int stationId,
        string name,
        bool isAutoPlay,
        CancellationToken cancellationToken = default)
    {
        var body = new
        {
            name,
            type   = "default",
            source = "songs",
            order  = isAutoPlay ? "shuffle" : "sequential",
            is_enabled = true,
            include_in_requests   = false,
            include_in_on_demand  = false,
            weight = 3
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"api/station/{stationId}/playlists", body, cancellationToken);
        EnsureAzuraCastSuccess(response, $"create playlist on station {stationId}");

        var result = await response.Content
            .ReadFromJsonAsync<AzuraCastApiPlaylistResponse>(cancellationToken);

        if (result == null) return null;

        _logger.LogInformation(
            "Created AzuraCast playlist '{Name}' (id: {Id}) for station {StationId}",
            result.Name, result.Id, stationId);

        return new AzuraCastPlaylistData { Id = result.Id, Name = result.Name ?? name };
    }

    public async Task<AzuraCastMediaData?> UploadMediaAsync(
        int stationId,
        Stream fileStream,
        string fileName,
        string title,
        string artist,
        string? album,
        CancellationToken cancellationToken = default)
    {
        using var content    = new MultipartFormDataContent();
        using var fileContent = new StreamContent(fileStream);
        content.Add(fileContent, "file", fileName);

        var response = await _httpClient.PostAsync(
            $"api/station/{stationId}/files", content, cancellationToken);
        EnsureAzuraCastSuccess(response, $"upload media to station {stationId}");

        var result = await response.Content
            .ReadFromJsonAsync<AzuraCastApiFileResponse>(cancellationToken);

        if (result?.UniqueId == null) return null;

        _logger.LogInformation(
            "Uploaded media '{Title}' to AzuraCast station {StationId} (uniqueId: {UniqueId})",
            result.Title ?? title, stationId, result.UniqueId);

        return new AzuraCastMediaData
        {
            UniqueId        = result.UniqueId,
            Path            = result.Path ?? fileName,
            Title           = result.Title ?? title,
            Artist          = result.Artist ?? artist,
            Album           = result.Album ?? album,
            DurationSeconds = (int)result.Length
        };
    }

    public async Task AssignMediaToPlaylistAsync(
        int stationId,
        string fileUniqueId,
        int playlistId,
        CancellationToken cancellationToken = default)
    {
        var body = new { playlists = new[] { playlistId } };

        var response = await _httpClient.PutAsJsonAsync(
            $"api/station/{stationId}/file/{fileUniqueId}", body, cancellationToken);
        EnsureAzuraCastSuccess(response, $"assign file to playlist on station {stationId}");

        _logger.LogInformation(
            "Assigned file {UniqueId} to playlist {PlaylistId} on station {StationId}",
            fileUniqueId, playlistId, stationId);
    }

    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    private static void EnsureAzuraCastSuccess(HttpResponseMessage response, string operation)
    {
        if (response.IsSuccessStatusCode) return;

        if (response.StatusCode == HttpStatusCode.Forbidden)
            throw new AzuraCastException(
                $"AzuraCast rejected '{operation}' with 403 Forbidden. " +
                "The configured API key lacks the required role. " +
                "Go to AzuraCast Admin ? API Keys and grant 'Manage Stations' + 'Manage Station Media' permissions.",
                ErrorCode.Forbidden);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new AzuraCastException(
                "AzuraCast returned 401 Unauthorized. Check that AzuraCast:ApiKey in appsettings is correct.",
                ErrorCode.Unauthorized);

        response.EnsureSuccessStatusCode();
    }
}
