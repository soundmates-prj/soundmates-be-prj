using System.Net.Http.Json;
using LiveSessionService.Application.Abstractions.ExternalServices;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Infrastructure.Services.AzuraCast;

/// <summary>
/// HTTP client for AzuraCast API
/// Fetches now playing and station data
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

    public async Task<AzuraCastNowPlayingDto?> GetNowPlayingAsync(
        string baseUrl,
        int stationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{baseUrl.TrimEnd('/')}/api/nowplaying/{stationId}";
            _logger.LogInformation("Fetching now playing from AzuraCast: {Url}", url);

            var response = await _httpClient.GetFromJsonAsync<AzuraCastNowPlayingDto>(
                url,
                cancellationToken);

            if (response == null)
            {
                _logger.LogWarning("Received null response from AzuraCast API");
                return null;
            }

            _logger.LogInformation(
                "Successfully fetched now playing from AzuraCast for station {StationId}",
                stationId);

            return response;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching now playing from AzuraCast: {Message}", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching now playing from AzuraCast");
            throw;
        }
    }
}
