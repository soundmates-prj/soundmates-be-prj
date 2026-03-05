using System.Net;
using System.Net.Http.Json;
using System.IO;
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
/// Ch? lo vi?c call API vù map response
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

        using var response = await _httpClient.PostAsJsonAsync(
            $"api/station/{stationId}/playlists", body, cancellationToken);
        await EnsureAzuraCastSuccessAsync(response, $"create playlist on station {stationId}", cancellationToken);

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
        string contentType,
        string title,
        string artist,
        string? album,
        CancellationToken cancellationToken = default)
    {
        // AzuraCast "Upload a new file" endpoint expects JSON with base64 content:
        // { path: "...", file: "..." } (see Api_UploadFile in AzuraCast OpenAPI spec).
        if (fileStream.CanSeek)
            fileStream.Position = 0;

        await using var ms = new MemoryStream();
        await fileStream.CopyToAsync(ms, cancellationToken);
        var bytes = ms.ToArray();

        var safeFileName = Path.GetFileName(fileName);
        var body = new
        {
            path = safeFileName,
            file = Convert.ToBase64String(bytes)
        };

        // Diagnostic: confirm X-API-Key header is present before sending
        bool hasApiKey = _httpClient.DefaultRequestHeaders.Contains("X-API-Key");
        string keyPreview = hasApiKey
            ? _httpClient.DefaultRequestHeaders.GetValues("X-API-Key").FirstOrDefault() is { } k && k.Length > 8
                ? k[..4] + "****" + k[^4..]
                : "****"
            : "(MISSING ó API key was not loaded at startup)";
        _logger.LogInformation(
            "UploadMediaAsync ? station {StationId} | X-API-Key header: {KeyPreview} | URL: {BaseAddress}api/station/{StationId}/files",
            stationId, keyPreview, _httpClient.BaseAddress, stationId);

        using var response = await _httpClient.PostAsJsonAsync(
            $"api/station/{stationId}/files", body, cancellationToken);
        await EnsureAzuraCastSuccessAsync(response, $"upload media to station {stationId}", cancellationToken);

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

        using var response = await _httpClient.PutAsJsonAsync(
            $"api/station/{stationId}/file/{fileUniqueId}", body, cancellationToken);
        await EnsureAzuraCastSuccessAsync(response, $"assign file to playlist on station {stationId}", cancellationToken);

        _logger.LogInformation(
            "Assigned file {UniqueId} to playlist {PlaylistId} on station {StationId}",
            fileUniqueId, playlistId, stationId);
    }

    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    private async Task EnsureAzuraCastSuccessAsync(
        HttpResponseMessage response,
        string operation,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;

        var status = response.StatusCode;
        var requestUrl =
            response.RequestMessage?.RequestUri?.ToString()
            ?? _httpClient.BaseAddress?.ToString()
            ?? "(unknown url)";

        string? body = null;
        try
        {
            body = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch
        {
            // ignore read failures
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            // AzuraCast returns 403 for two distinct reasons:
            // - NotLoggedInException : API key is missing or not recognised
            // - ForbiddenException   : API key is valid but lacks the required permission
            bool isNotLoggedIn = !string.IsNullOrWhiteSpace(body) &&
                body.Contains("NotLoggedInException", StringComparison.OrdinalIgnoreCase);

            string reason = isNotLoggedIn
                ? $"AzuraCast rejected '{operation}' with 403 ó API key not recognised (NotLoggedInException). " +
                  "The X-API-Key header was either missing or the key does not exist in AzuraCast. " +
                  "Check that AzuraCast__ApiKey in your .env matches an API key in AzuraCast Admin ? API Keys."
                : $"AzuraCast rejected '{operation}' with 403 Forbidden ó the API key lacks the required role. " +
                  "Go to AzuraCast Admin ? API Keys and grant 'Manage Stations' + 'Manage Station Media' permissions.";

            throw new AzuraCastException(
                reason + (string.IsNullOrWhiteSpace(body) ? string.Empty : $" Response: {TruncateForError(body)}"),
                ErrorCode.Forbidden);
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new AzuraCastException(
                "AzuraCast returned 401 Unauthorized. Check that AzuraCast:ApiKey in appsettings is correct." +
                (string.IsNullOrWhiteSpace(body) ? string.Empty : $" Response: {TruncateForError(body)}"),
                ErrorCode.Unauthorized);

        var mapped = status switch
        {
            HttpStatusCode.BadRequest => ErrorCode.BadRequest,
            HttpStatusCode.NotFound => ErrorCode.NotFound,
            HttpStatusCode.UnprocessableEntity => ErrorCode.UnprocessableEntity,
            HttpStatusCode.ServiceUnavailable => ErrorCode.ServiceUnavailable,
            _ when (int)status >= 500 => ErrorCode.InternalServerError,
            _ => ErrorCode.BadRequest
        };

        _logger.LogError(
            "AzuraCast non-success response. Operation={Operation} Status={StatusCode} Url={Url} Body={Body}",
            operation, (int)status, requestUrl, body);

        var message =
            $"AzuraCast request failed during '{operation}' ({(int)status} {status}). " +
            $"Url: {requestUrl}." +
            (string.IsNullOrWhiteSpace(body) ? string.Empty : $" Response: {TruncateForError(body)}");

        throw new AzuraCastException(message, mapped);
    }

    private static string TruncateForError(string value, int maxLen = 2000)
    {
        value = value.Trim();
        return value.Length <= maxLen ? value : value.Substring(0, maxLen) + "...";
    }
}
