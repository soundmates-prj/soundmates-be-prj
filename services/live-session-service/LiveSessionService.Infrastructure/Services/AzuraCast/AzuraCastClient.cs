using System.Net;
using System.Net.Http.Json;
using System.IO;
using System.Text.Json;
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
/// Ch? lo vi?c call API v� map response
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
        var endpoint = $"api/nowplaying/{stationId}";
        _logger.LogInformation(
            "Fetching now playing from AzuraCast for station {StationId}",
            stationId);

        using var response = await _httpClient.GetAsync(endpoint, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new AzuraCastException(
                $"AzuraCast now-playing not found for station {stationId}. Station may not exist or has no now-playing endpoint.",
                ErrorCode.NotFound);
        }

        await EnsureAzuraCastSuccessAsync(response, $"get now-playing for station {stationId}", cancellationToken);

        var apiResponse = await response.Content.ReadFromJsonAsync<AzuraCastApiResponse>(cancellationToken);

        if (apiResponse == null)
        {
            _logger.LogWarning(
                "Received null now-playing response from AzuraCast for station {StationId}",
                stationId);
            return null;
        }

        var applicationModel = apiResponse.ToApplicationModel();

        _logger.LogInformation(
            "Successfully fetched now playing from AzuraCast for station {StationId}",
            stationId);

        return applicationModel;
    }

    public async Task<AzuraCastStationListData?> CreateStationAsync(
        string name,
        string? shortCode,
        string? description,
        int port,
        CancellationToken cancellationToken = default)
    {
        var body = new
        {
            name,
            short_name = shortCode,
            description,
            frontend_type = "icecast",
            backend_type = "liquidsoap",
            frontend_config = new { port }
        };

        using var response = await _httpClient.PostAsJsonAsync(
            "api/admin/stations", body, cancellationToken);
        
        await EnsureAzuraCastSuccessAsync(response, "create station", cancellationToken);

        var result = await response.Content
            .ReadFromJsonAsync<AzuraCastApiStationListResponse>(cancellationToken);

        if (result == null) return null;

        _logger.LogInformation(
            "Created AzuraCast station '{Name}' (id: {Id})",
            result.Name, result.Id);

        return result.ToApplicationModel();
    }

    public async Task<List<AzuraCastPlaylistData>> GetStationPlaylistsAsync(
        int stationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoint = $"api/station/{stationId}/playlists";
            _logger.LogInformation(
                "Fetching playlists from AzuraCast for station {StationId}",
                stationId);

            var apiResponse = await _httpClient.GetFromJsonAsync<List<AzuraCastApiPlaylistResponse>>(
                endpoint,
                cancellationToken);

            if (apiResponse == null || apiResponse.Count == 0)
            {
                _logger.LogWarning(
                    "Received empty or null playlists response from AzuraCast for station {StationId}",
                    stationId);
                return new List<AzuraCastPlaylistData>();
            }

            var playlists = apiResponse.Select(p => new AzuraCastPlaylistData
            {
                Id = p.Id,
                Name = p.Name ?? "Unnamed",
                Description = p.Description,
                Type = p.Type,
                Source = p.Source,
                Order = p.Order,
                IsEnabled = p.IsEnabled,
                IncludeInRequests = p.IncludeInRequests,
                IncludeInOnDemand = p.IncludeInOnDemand,
                Weight = p.Weight
            }).ToList();

            _logger.LogInformation(
                "Successfully fetched {Count} playlists from AzuraCast for station {StationId}",
                playlists.Count, stationId);

            return playlists;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "HTTP error fetching playlists from AzuraCast for station {StationId}: {Message}",
                stationId, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error fetching playlists from AzuraCast for station {StationId}",
                stationId);
            throw;
        }
    }

    public async Task<List<AzuraCastStationFileData>> GetStationFilesAsync(
        int stationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoint = $"api/station/{stationId}/files";
            _logger.LogInformation(
                "Fetching media files from AzuraCast for station {StationId}",
                stationId);

            var apiResponse = await _httpClient.GetFromJsonAsync<List<AzuraCastApiStationFileResponse>>(
                endpoint,
                cancellationToken);

            if (apiResponse == null || apiResponse.Count == 0)
            {
                _logger.LogWarning(
                    "Received empty or null files response from AzuraCast for station {StationId}",
                    stationId);
                return new List<AzuraCastStationFileData>();
            }

            var files = apiResponse.Select(f => new AzuraCastStationFileData
            {
                Id = f.Id,
                UniqueId = f.UniqueId ?? string.Empty,
                SongId = f.SongId,
                Text = f.Text,
                Artist = f.Artist,
                Title = f.Title,
                Album = f.Album,
                Genre = f.Genre,
                Isrc = f.Isrc,
                Lyrics = f.Lyrics,
                Art = f.Art,
                Path = f.Path ?? string.Empty,
                Mtime = f.Mtime,
                UploadedAt = f.UploadedAt,
                ArtUpdatedAt = f.ArtUpdatedAt,
                Length = f.Length,
                LengthText = f.LengthText,
                Playlists = f.Playlists?.Select(p => new AzuraCastFilePlaylistInfo
                {
                    Id = p.Id,
                    Name = p.Name,
                    ShortName = p.ShortName,
                    Folder = p.Folder,
                    Count = p.Count
                }).ToList()
            }).ToList();

            _logger.LogInformation(
                "Successfully fetched {Count} media files from AzuraCast for station {StationId}",
                files.Count, stationId);

            return files;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "HTTP error fetching media files from AzuraCast for station {StationId}: {Message}",
                stationId, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error fetching media files from AzuraCast for station {StationId}",
                stationId);
            throw;
        }
    }

    public async Task<AzuraCastPlaylistData?> CreatePlaylistAsync(
        int stationId,
        string name,
        string? description,
        bool isAutoPlay,
        bool includeInRequests,
        string songPlaybackOrder,
        CancellationToken cancellationToken = default)
    {
        var body = new
        {
            name,
            description,
            type = "default",
            source = "songs",
            order = NormalizePlaylistOrder(songPlaybackOrder),
            is_enabled = true,
            include_in_requests = includeInRequests,
            include_in_on_demand = false,
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

        return new AzuraCastPlaylistData
        {
            Id = result.Id,
            Name = result.Name ?? name,
            Description = result.Description ?? description
        };
    }

    public async Task<AzuraCastPlaylistData?> UpdatePlaylistAsync(
        int stationId,
        int playlistId,
        string name,
        string? description,
        bool isAutoPlay,
        bool includeInRequests,
        bool includeInOnDemand,
        bool isEnabled,
        string songPlaybackOrder,
        CancellationToken cancellationToken = default)
    {
        var body = new
        {
            name,
            description,
            type = "default",
            source = "songs",
            order = NormalizePlaylistOrder(songPlaybackOrder),
            is_enabled = isEnabled,
            include_in_requests = includeInRequests,
            include_in_on_demand = includeInOnDemand
        };

        using var response = await _httpClient.PutAsJsonAsync(
            $"api/station/{stationId}/playlist/{playlistId}", body, cancellationToken);

        await EnsureAzuraCastSuccessAsync(response, $"update playlist {playlistId} on station {stationId}", cancellationToken);

        var result = await response.Content
            .ReadFromJsonAsync<AzuraCastApiPlaylistResponse>(cancellationToken);

        if (result == null) return null;

        _logger.LogInformation(
            "Updated AzuraCast playlist '{Name}' (id: {Id}) for station {StationId}",
            result.Name, result.Id, stationId);

        return new AzuraCastPlaylistData
        {
            Id = result.Id,
            Name = result.Name ?? name,
            Description = result.Description ?? description
        };
    }

    public async Task DeletePlaylistAsync(
        int stationId,
        int playlistId,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.DeleteAsync(
            $"api/station/{stationId}/playlist/{playlistId}",
            cancellationToken);

        await EnsureAzuraCastSuccessAsync(response, $"delete playlist {playlistId} on station {stationId}", cancellationToken);

        _logger.LogInformation(
            "Deleted AzuraCast playlist {PlaylistId} on station {StationId}",
            playlistId,
            stationId);
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
            : "(MISSING � API key was not loaded at startup)";
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

    public async Task UpdateMediaMetadataAsync(
        int stationId,
        string fileUniqueId,
        string title,
        string? artist,
        string? album,
        string? lyrics,
        CancellationToken cancellationToken = default)
    {
        var body = new
        {
            title,
            artist,
            album,
            lyrics
        };

        var encodedUniqueId = Uri.EscapeDataString(fileUniqueId);
        using var byUniqueIdResponse = await _httpClient.PutAsJsonAsync(
            $"api/station/{stationId}/file/{encodedUniqueId}",
            body,
            cancellationToken);

        if (byUniqueIdResponse.StatusCode != HttpStatusCode.NotFound)
        {
            await EnsureAzuraCastSuccessAsync(
                byUniqueIdResponse,
                $"update media metadata for file {fileUniqueId} on station {stationId}",
                cancellationToken);

            _logger.LogInformation(
                "Updated AzuraCast metadata for file {UniqueId} on station {StationId}",
                fileUniqueId,
                stationId);
            return;
        }

        // Fallback for AzuraCast setups that only accept numeric file id in the route.
        var stationFiles = await GetStationFilesAsync(stationId, cancellationToken);
        var matched = stationFiles.FirstOrDefault(f =>
            string.Equals(f.UniqueId, fileUniqueId, StringComparison.OrdinalIgnoreCase));

        if (matched == null)
        {
            throw new AzuraCastException(
                $"Media file '{fileUniqueId}' was not found on AzuraCast station {stationId} for metadata update.",
                ErrorCode.NotFound);
        }

        using var byNumericIdResponse = await _httpClient.PutAsJsonAsync(
            $"api/station/{stationId}/file/{matched.Id}",
            body,
            cancellationToken);

        await EnsureAzuraCastSuccessAsync(
            byNumericIdResponse,
            $"update media metadata for file {matched.Id} on station {stationId}",
            cancellationToken);

        _logger.LogInformation(
            "Updated AzuraCast metadata for file {UniqueId} (id: {FileId}) on station {StationId}",
            fileUniqueId,
            matched.Id,
            stationId);
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

    public async Task RemoveMediaFromPlaylistAsync(
        int stationId,
        string fileUniqueId,
        int playlistId,
        CancellationToken cancellationToken = default)
    {
        var stationFiles = await GetStationFilesAsync(stationId, cancellationToken);
        var stationFile = stationFiles.FirstOrDefault(f =>
            string.Equals(f.UniqueId, fileUniqueId, StringComparison.OrdinalIgnoreCase));

        if (stationFile == null)
        {
            throw new AzuraCastException(
                $"Media file '{fileUniqueId}' was not found on AzuraCast station {stationId}.",
                ErrorCode.NotFound);
        }

        var updatedPlaylistIds = stationFile.Playlists?
            .Select(p => p.Id)
            .Where(id => id != playlistId)
            .Distinct()
            .ToArray() ?? Array.Empty<int>();

        var body = new { playlists = updatedPlaylistIds };

        using var response = await _httpClient.PutAsJsonAsync(
            $"api/station/{stationId}/file/{fileUniqueId}", body, cancellationToken);
        await EnsureAzuraCastSuccessAsync(response, $"remove file from playlist on station {stationId}", cancellationToken);

        _logger.LogInformation(
            "Removed file {UniqueId} from playlist {PlaylistId} on station {StationId}",
            fileUniqueId, playlistId, stationId);
    }

    public async Task QueueSongRequestAsync(
        int stationId,
        string mediaUniqueId,
        CancellationToken cancellationToken = default)
    {
        var stationFiles = await GetStationFilesAsync(stationId, cancellationToken);
        var stationFile = stationFiles.FirstOrDefault(f =>
            string.Equals(f.UniqueId, mediaUniqueId, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(f.Path, mediaUniqueId, StringComparison.OrdinalIgnoreCase));

        var candidateIds = new List<string>();

        if (!string.IsNullOrWhiteSpace(stationFile?.SongId))
            candidateIds.Add(stationFile!.SongId!);

        if (stationFile is not null)
            candidateIds.Add(stationFile.Id.ToString());

        if (!string.IsNullOrWhiteSpace(stationFile?.UniqueId))
            candidateIds.Add(stationFile!.UniqueId);

        if (!string.IsNullOrWhiteSpace(mediaUniqueId))
            candidateIds.Add(mediaUniqueId);

        candidateIds = candidateIds
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (candidateIds.Count == 0)
        {
            throw new AzuraCastException(
                $"No valid media identifier for song request on station {stationId}.",
                ErrorCode.BadRequest);
        }

        var triedIds = new List<string>();

        foreach (var candidate in candidateIds)
        {
            var encodedMediaId = Uri.EscapeDataString(candidate);
            using var response = await _httpClient.PostAsync(
                $"api/station/{stationId}/request/{encodedMediaId}",
                content: null,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Queued AzuraCast song request for media {MediaUniqueId} using identifier {RequestMediaId} on station {StationId}",
                    mediaUniqueId,
                    candidate,
                    stationId);
                return;
            }

            triedIds.Add(candidate);

            if (response.StatusCode != HttpStatusCode.NotFound && response.StatusCode != HttpStatusCode.MethodNotAllowed)
            {
                await EnsureAzuraCastSuccessAsync(response, $"queue song request on station {stationId}", cancellationToken);
            }
        }

        throw new AzuraCastException(
            $"Song cannot be requested on station {stationId}. Tried identifiers: {string.Join(", ", triedIds)}",
            ErrorCode.BadRequest);
    }

    public async Task DeleteMediaAsync(
        int stationId,
        string fileUniqueId,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.DeleteAsync(
            $"api/station/{stationId}/file/{fileUniqueId}", cancellationToken);

        await EnsureAzuraCastSuccessAsync(response, $"delete media on station {stationId}", cancellationToken);

        _logger.LogInformation(
            "Deleted file {UniqueId} on station {StationId}",
            fileUniqueId,
            stationId);
    }

    public async Task SkipTrackAsync(int stationId, CancellationToken cancellationToken = default)
    {
        var endpoint = $"api/station/{stationId}/backend/skip";
        _logger.LogInformation("Skipping track for station {StationId}", stationId);

        using var response = await _httpClient.PostAsync(endpoint, null, cancellationToken);
        await EnsureAzuraCastSuccessAsync(response, $"skip track for station {stationId}", cancellationToken);

        _logger.LogInformation("Successfully skipped track for station {StationId}", stationId);
    }

    public async Task<List<AzuraCastCurrentSongData>> GetUpcomingQueueAsync(int stationId, CancellationToken cancellationToken = default)
    {
        var endpoint = $"api/station/{stationId}/queue";
        _logger.LogInformation("Fetching upcoming queue for station {StationId}", stationId);

        var apiResponse = await _httpClient.GetFromJsonAsync<List<AzuraCastApiNowPlaying>>(endpoint, cancellationToken);

        if (apiResponse == null)
        {
            return new List<AzuraCastCurrentSongData>();
        }

        return apiResponse.Select(q => q.ToApplicationModel()).ToList();
    }

    public async Task RestartStationAsync(int stationId, CancellationToken cancellationToken = default)
    {
        var endpoint = $"api/station/{stationId}/restart";
        _logger.LogInformation("Restarting station {StationId}", stationId);

        using var response = await _httpClient.PostAsync(endpoint, new StringContent("{}", System.Text.Encoding.UTF8, "application/json"), cancellationToken);
        await EnsureAzuraCastSuccessAsync(response, $"restart station {stationId}", cancellationToken);

        _logger.LogInformation("Successfully requested restart for station {StationId}", stationId);
    }

    public async Task ReloadStationAsync(int stationId, CancellationToken cancellationToken = default)
    {
        var endpoint = $"api/station/{stationId}/reload";
        _logger.LogInformation("Reloading broadcasting config for station {StationId}", stationId);

        using var response = await _httpClient.PostAsync(endpoint, new StringContent("{}", System.Text.Encoding.UTF8, "application/json"), cancellationToken);
        await EnsureAzuraCastSuccessAsync(response, $"reload station {stationId}", cancellationToken);

        _logger.LogInformation("Successfully requested reload for station {StationId}", stationId);
    }

    public async Task<(byte[] Content, string? ContentType, string? FileName)?> DownloadMediaAsync(
        int stationId,
        string fileUniqueId,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            $"api/station/{stationId}/file/{fileUniqueId}/download",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureAzuraCastSuccessAsync(response, $"download media on station {stationId}", cancellationToken);

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var contentType = response.Content.Headers.ContentType?.MediaType;
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                       ?? response.Content.Headers.ContentDisposition?.FileName;

        return (bytes, contentType, fileName);
    }

    private static string NormalizePlaylistOrder(string? songPlaybackOrder)
    {
        return songPlaybackOrder?.Trim().ToLowerInvariant() switch
        {
            "shuffled" => "shuffle",
            "shuffle" => "shuffle",
            "random" => "random",
            "sequential" => "sequential",
            "sequence" => "sequential",
            _ => "sequential"
        };
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
                ? $"AzuraCast rejected '{operation}' with 403 � API key not recognised (NotLoggedInException). " +
                  "The X-API-Key header was either missing or the key does not exist in AzuraCast. " +
                  "Check that AzuraCast__ApiKey in your .env matches an API key in AzuraCast Admin ? API Keys."
                : $"AzuraCast rejected '{operation}' with 403 Forbidden � the API key lacks the required role. " +
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

        if (response.StatusCode == HttpStatusCode.InternalServerError &&
            TryMapAzuraCastBusinessError(body, out var businessMessage, out var businessCode))
        {
            throw new AzuraCastException(
                $"AzuraCast rejected '{operation}': {businessMessage}",
                businessCode);
        }

        var mapped = status switch
        {
            HttpStatusCode.BadRequest => ErrorCode.BadRequest,
            HttpStatusCode.NotFound => ErrorCode.NotFound,
            HttpStatusCode.Conflict => ErrorCode.Conflict,
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

    private static bool TryMapAzuraCastBusinessError(
        string? body,
        out string message,
        out ErrorCode errorCode)
    {
        message = "Request cannot be completed by AzuraCast.";
        errorCode = ErrorCode.BadRequest;

        if (string.IsNullOrWhiteSpace(body))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            var type = root.TryGetProperty("type", out var typeElement)
                ? typeElement.GetString()
                : null;

            var apiMessage = root.TryGetProperty("message", out var msgElement)
                ? msgElement.GetString()
                : null;

            if (string.Equals(type, "CannotCompleteActionException", StringComparison.OrdinalIgnoreCase))
            {
                message = apiMessage ?? message;
                errorCode = ErrorCode.Conflict;
                return true;
            }
        }
        catch
        {
            // ignore parse errors
        }

        return false;
    }

    private static string TruncateForError(string value, int maxLen = 2000)
    {
        value = value.Trim();
        return value.Length <= maxLen ? value : value.Substring(0, maxLen) + "...";
    }

    /// <inheritdoc />
    public void UpdateConfig(string baseUrl, string apiKey)
    {
        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            _httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        }

        _httpClient.DefaultRequestHeaders.Remove("X-API-Key");
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);
        }

        _logger.LogInformation(
            "AzuraCast client config updated at runtime: BaseUrl={BaseUrl}, ApiKey={MaskedKey}",
            _httpClient.BaseAddress,
            !string.IsNullOrWhiteSpace(apiKey) && apiKey.Length > 8
                ? apiKey[..4] + "****"
                : "(set)");
    }
}
